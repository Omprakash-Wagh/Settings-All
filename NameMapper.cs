using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Flow.Launcher.Plugin;

namespace SettingsAll;

public static class NameMapper
{
    private static Dictionary<string, SettingsEntry> _seedDict;
    private static string _seedHash;

    public static string SeedHash => _seedHash;

    public static void LoadSeedDictionary(IPublicAPI api, string pluginDirectory)
    {
        try
        {
            string path = Path.Combine(pluginDirectory, "Resources", "friendly_names.json");
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    _seedHash = Convert.ToHexString(sha.ComputeHash(bytes));
                }

                var entries = JsonSerializer.Deserialize<List<SettingsEntry>>(bytes);
                if (entries != null)
                {
                    _seedDict = new Dictionary<string, SettingsEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in entries)
                    {
                        if (DllScanner.IsValidUri(entry.Uri))
                        {
                            _seedDict[entry.Uri] = entry;
                        }
                        else
                        {
                            api.LogWarn("Settings All", $"Invalid URI in seed dictionary: {entry.Uri}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            api.LogWarn("Settings All", $"Failed to load seed dictionary: {ex.Message}");
        }

        if (_seedDict == null)
        {
            _seedDict = new Dictionary<string, SettingsEntry>();
            if (string.IsNullOrEmpty(_seedHash))
                _seedHash = "fallback";
        }
    }

    public static List<SettingsEntry> MapToEntries(HashSet<string> scannedUris)
    {
        var results = new List<SettingsEntry>();

        foreach (var uri in scannedUris)
        {
            if (_seedDict != null && _seedDict.TryGetValue(uri, out var seedEntry))
            {
                results.Add(new SettingsEntry
                {
                    Uri = uri,
                    FriendlyName = seedEntry.FriendlyName,
                    Category = seedEntry.Category,
                    Aliases = seedEntry.Aliases,
                    IsFromSeedDictionary = true
                });
            }
            else
            {
                results.Add(AutoHumanize(uri));
            }
        }

        return results;
    }

    private static SettingsEntry AutoHumanize(string uri)
    {
        var span = uri.AsSpan("ms-settings:".Length);
        int qMark = span.IndexOf('?');
        var pathSegment = qMark >= 0 ? span[..qMark] : span;

        string[] parts = pathSegment.ToString().Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                parts[i] = char.ToUpperInvariant(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : "");
            }
        }

        string friendlyName = string.Join(" ", parts);
        if (string.IsNullOrWhiteSpace(friendlyName))
            friendlyName = "Settings";

        string category = InferCategory(parts);

        return new SettingsEntry
        {
            Uri = uri,
            FriendlyName = friendlyName,
            Category = category,
            Aliases = Array.Empty<string>(),
            IsFromSeedDictionary = false
        };
    }

    private static string InferCategory(string[] parts)
    {
        if (parts.Length == 0) return "Other";

        string first = parts[0].ToLowerInvariant();
        return first switch
        {
            "network" => "Network & Internet",
            "privacy" => "Privacy & Security",
            "easeofaccess" => "Accessibility",
            "gaming" => "Gaming",
            "personalization" => "Personalization",
            "apps" => "Apps",
            _ => "Other"
        };
    }
}
