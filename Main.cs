using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin;

namespace SettingsAll;

public class Main : IAsyncPlugin, IContextMenu, ISettingProvider, IAsyncDisposable
{
    private PluginInitContext _context;
    private PluginSettings _settings;
    private volatile IReadOnlyList<SettingsEntry> _catalog;
    
    private readonly SemaphoreSlim _rebuildSemaphore = new(1, 1);
    private readonly ConcurrentQueue<Task> _activeRebuilds = new();
    private readonly CancellationTokenSource _disposeCts = new();
    
    private readonly object _pendingLock = new();
    private bool _pendingBypassDedupGuard;
    private bool _pendingBypassCache;
    
    private DateTime _lastRebuildUtc = DateTime.MinValue;

    public async Task InitAsync(PluginInitContext context)
    {
        _context = context;
        _settings = context.API.LoadSettingJsonStorage<PluginSettings>();

        NameMapper.LoadSeedDictionary(_context.API, _context.CurrentPluginMetadata.PluginDirectory);

        _activeRebuilds.Enqueue(Task.Run(() => RebuildCatalogAsync(bypassDedupGuard: false, bypassCache: false)));
        
        await Task.CompletedTask;
    }

    private async Task RebuildCatalogAsync(bool bypassDedupGuard, bool bypassCache)
    {
        lock (_pendingLock)
        {
            bypassDedupGuard |= _pendingBypassDedupGuard;
            bypassCache |= _pendingBypassCache;
            _pendingBypassDedupGuard = false;
            _pendingBypassCache = false;
        }

        bool acquired = false;
        try
        {
            if (bypassDedupGuard)
            {
                acquired = await _rebuildSemaphore.WaitAsync(0, _disposeCts.Token);
                if (!acquired)
                {
                    lock (_pendingLock)
                    {
                        _pendingBypassDedupGuard |= bypassDedupGuard;
                        _pendingBypassCache |= bypassCache;
                    }
                    _context.API.ShowMsg("Settings All", "Rescan already in progress...", _context.CurrentPluginMetadata.IcoPath);
                    return;
                }
            }
            else
            {
                await _rebuildSemaphore.WaitAsync(_disposeCts.Token);
                acquired = true;
            }

            int loopCount = 0;
            while (true)
            {
                bool breakLoop = false;
                try
                {
                    string dllPath = _settings.DllPath;

                    if (!bypassDedupGuard && (DateTime.UtcNow - _lastRebuildUtc).TotalSeconds < 5)
                    {
                        return;
                    }

                    string cachePath = Path.Combine(_context.CurrentPluginMetadata.PluginDirectory, "settings_cache.json");

                    if (bypassCache)
                    {
                        try { File.Delete(cachePath); } catch { }
                    }

                    if (!File.Exists(dllPath))
                    {
                        _context.API.LogWarn("Settings All", $"DLL not found at {dllPath}, falling back to embedded list.");
                        _catalog = FallbackEntries.Entries;
                        _lastRebuildUtc = DateTime.UtcNow;
                        return; // exit loop and release
                    }

                    if (!bypassCache && CacheManager.TryLoadCache(cachePath, dllPath, NameMapper.SeedHash, out var cachedEntries))
                    {
                        _catalog = cachedEntries;
                        _lastRebuildUtc = DateTime.UtcNow;
                    }
                    else
                    {
                        var uris = DllScanner.ScanFile(dllPath, _disposeCts.Token);
                        if (uris.Count == 0)
                        {
                            _context.API.LogWarn("Settings All", "Scan failed or returned 0 entries. Falling back to embedded list.");
                            _catalog = FallbackEntries.Entries;
                        }
                        else
                        {
                            var entries = NameMapper.MapToEntries(uris);
                            _catalog = entries;
                            _lastRebuildUtc = DateTime.UtcNow;

                            _context.API.LogInfo("Settings All", $"Scanned {entries.Count} URIs from {dllPath}");
                            if (entries.Count < 50)
                            {
                                _context.API.LogWarn("Settings All", $"Unusually low URI count ({entries.Count}), DLL may have changed structure.");
                            }

                            CacheManager.SaveCache(cachePath, dllPath, NameMapper.SeedHash, entries);
                        }
                    }
                }
                finally
                {
                    if (_disposeCts.IsCancellationRequested)
                    {
                        breakLoop = true;
                    }
                    else
                    {
                        lock (_pendingLock)
                        {
                            if (_pendingBypassDedupGuard || _pendingBypassCache)
                            {
                                if (loopCount < 3)
                                {
                                    bypassDedupGuard = _pendingBypassDedupGuard;
                                    bypassCache = _pendingBypassCache;
                                    _pendingBypassDedupGuard = false;
                                    _pendingBypassCache = false;
                                    loopCount++;
                                }
                                else
                                {
                                    breakLoop = true; // Starvation cap hit
                                }
                            }
                            else
                            {
                                breakLoop = true; // No pending requests
                            }
                        }
                    }
                }
                
                if (breakLoop) break;
            } // end while
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _context.API.LogException("Settings All", "Rebuild failed", ex);
        }
        finally
        {
            if (acquired)
            {
                try { _rebuildSemaphore.Release(); } catch (ObjectDisposedException) { }
            }
        }
    }

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var catalog = _catalog;
        if (catalog == null)
        {
            return new List<Result>
            {
                new()
                {
                    Title = "Loading settings catalog...",
                    SubTitle = "Please wait...",
                    IcoPath = _context.CurrentPluginMetadata.IcoPath
                }
            };
        }

        string search = query.Search;
        if (string.IsNullOrWhiteSpace(search))
        {
            return catalog.OrderBy(e => e.FriendlyName)
                          .Take(50)
                          .Select(e => CreateResult(e))
                          .ToList();
        }

        var results = new List<(SettingsEntry Entry, int SortScore, bool HasHit)>();

        foreach (var entry in catalog)
        {
            token.ThrowIfCancellationRequested();

            if (ScoreCalculator.TryCalculateScore(entry, search, (q, t) => _context.API.FuzzySearch(q, t).Score, out int sortScore, out bool hasHit))
            {
                results.Add((entry, sortScore, hasHit));
            }
        }

        return results.OrderByDescending(r => r.HasHit)
                      .ThenByDescending(r => r.SortScore)
                      .Select(r => CreateResult(r.Entry, r.SortScore))
                      .ToList();
    }

    private Result CreateResult(SettingsEntry entry, int score = 0)
    {
        return new Result
        {
            Title = entry.FriendlyName,
            SubTitle = $"{entry.Category} · {entry.Uri}",
            IcoPath = _context.CurrentPluginMetadata.IcoPath,
            Score = score,
            Action = _ =>
            {
                if (!DllScanner.IsValidUri(entry.Uri))
                {
                    _context.API.LogWarn("Settings All", $"Invalid URI attempted to launch: {entry.Uri}");
                    return true;
                }

                try
                {
                    Process.Start(new ProcessStartInfo { FileName = entry.Uri, UseShellExecute = true });
                }
                catch (Exception)
                {
                    _context.API.ShowMsg("Failed to open", "This settings page may not be available on your Windows edition/build.", _context.CurrentPluginMetadata.IcoPath);
                }
                return true;
            }
        };
    }

    public List<Result> LoadContextMenus(Result selectedResult)
    {
        if (selectedResult.SubTitle == "Please wait...") return new List<Result>();

        var uri = selectedResult.SubTitle.Split('·').Last().Trim();
        return new List<Result>
        {
            new()
            {
                Title = "Copy URI",
                SubTitle = "Copy the ms-settings URI to clipboard",
                IcoPath = _context.CurrentPluginMetadata.IcoPath,
                Action = _ =>
                {
                    System.Windows.Clipboard.SetText(uri);
                    return true;
                }
            },
            new()
            {
                Title = "Force Rescan",
                SubTitle = "Bypass cache and force a fresh scan of SystemSettings.dll",
                IcoPath = _context.CurrentPluginMetadata.IcoPath,
                Action = _ =>
                {
                    _activeRebuilds.Enqueue(Task.Run(() => RebuildCatalogAsync(bypassDedupGuard: true, bypassCache: true)));
                    return true;
                }
            }
        };
    }

    public Control CreateSettingPanel()
    {
        return new SettingsView(_settings, () =>
        {
            _activeRebuilds.Enqueue(Task.Run(() => RebuildCatalogAsync(bypassDedupGuard: true, bypassCache: false)));
        });
    }

    public async ValueTask DisposeAsync()
    {
        _disposeCts.Cancel();
        
        try
        {
            await Task.WhenAny(Task.WhenAll(_activeRebuilds), Task.Delay(500));
        }
        catch { }
        
        _rebuildSemaphore.Dispose();
        _disposeCts.Dispose();
    }
}
