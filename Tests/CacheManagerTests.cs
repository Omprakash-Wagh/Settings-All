using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SettingsAll.Tests;

public class CacheManagerTests : IDisposable
{
    private string _testDir;
    private string _dllPath;
    private string _cachePath;

    public CacheManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SettingsAllTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
        
        _dllPath = Path.Combine(_testDir, "mock.dll");
        File.WriteAllText(_dllPath, "fake dll");
        
        _cachePath = Path.Combine(_testDir, "settings_cache.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void Test1_CacheHit()
    {
        var entries = new List<SettingsEntry> { new SettingsEntry { Uri = "ms-settings:test" } };
        CacheManager.SaveCache(_cachePath, _dllPath, "hash123", entries);
        
        bool hit = CacheManager.TryLoadCache(_cachePath, _dllPath, "hash123", out var loaded);
        Assert.True(hit);
        Assert.Single(loaded);
        Assert.Equal("ms-settings:test", loaded[0].Uri);
    }

    [Fact]
    public void Test3_TimestampChange()
    {
        var entries = new List<SettingsEntry> { new SettingsEntry { Uri = "ms-settings:test" } };
        CacheManager.SaveCache(_cachePath, _dllPath, "hash123", entries);
        
        File.SetLastWriteTimeUtc(_dllPath, DateTime.UtcNow.AddDays(1));
        
        bool hit = CacheManager.TryLoadCache(_cachePath, _dllPath, "hash123", out _);
        Assert.False(hit);
    }

    [Fact]
    public void Test4_SeedHashChange()
    {
        var entries = new List<SettingsEntry> { new SettingsEntry { Uri = "ms-settings:test" } };
        CacheManager.SaveCache(_cachePath, _dllPath, "hash123", entries);
        
        bool hit = CacheManager.TryLoadCache(_cachePath, _dllPath, "hash456", out _);
        Assert.False(hit);
    }

    [Fact]
    public void Test5_CorruptedJson()
    {
        File.WriteAllText(_cachePath, "invalid json");
        
        bool hit = CacheManager.TryLoadCache(_cachePath, _dllPath, "hash123", out _);
        Assert.False(hit);
        Assert.False(File.Exists(_cachePath));
    }

    [Fact]
    public void Test6_RoundTrip()
    {
        var entries = new List<SettingsEntry> 
        { 
            new SettingsEntry 
            { 
                Uri = "ms-settings:test", 
                FriendlyName = "Test", 
                Category = "Cat", 
                Aliases = new[] { "t" }, 
                IsFromSeedDictionary = true 
            } 
        };
        CacheManager.SaveCache(_cachePath, _dllPath, "hash123", entries);
        
        CacheManager.TryLoadCache(_cachePath, _dllPath, "hash123", out var loaded);
        var entry = loaded[0];
        Assert.Equal("ms-settings:test", entry.Uri);
        Assert.Equal("Test", entry.FriendlyName);
        Assert.Equal("Cat", entry.Category);
        Assert.Single(entry.Aliases);
        Assert.True(entry.IsFromSeedDictionary);
    }
}
