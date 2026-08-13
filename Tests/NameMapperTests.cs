using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Flow.Launcher.Plugin;
using Moq;

namespace SettingsAll.Tests;

public class NameMapperTests : IDisposable
{
    private string _testDir;
    private Mock<IPublicAPI> _apiMock;

    public NameMapperTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "SettingsAllTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
        
        var resDir = Path.Combine(_testDir, "Resources");
        Directory.CreateDirectory(resDir);
        
        _apiMock = new Mock<IPublicAPI>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [Fact]
    public void Test1_SeedDictionaryHit()
    {
        string json = @"[{""uri"":""ms-settings:network-mobilehotspot"",""friendlyName"":""Mobile Hotspot"",""category"":""Network & Internet"",""aliases"":[""hotspot""]}]";
        File.WriteAllText(Path.Combine(_testDir, "Resources", "friendly_names.json"), json);
        
        NameMapper.LoadSeedDictionary(_apiMock.Object, _testDir);
        
        var uris = new HashSet<string> { "ms-settings:network-mobilehotspot" };
        var res = NameMapper.MapToEntries(uris);
        
        Assert.Single(res);
        Assert.Equal("Mobile Hotspot", res[0].FriendlyName);
        Assert.Equal("Network & Internet", res[0].Category);
        Assert.True(res[0].IsFromSeedDictionary);
    }

    [Fact]
    public void Test2_AutoHumanization()
    {
        NameMapper.LoadSeedDictionary(_apiMock.Object, _testDir); // Empty dir, no dict
        
        var uris = new HashSet<string> { "ms-settings:foobar-bazqux" };
        var res = NameMapper.MapToEntries(uris);
        
        Assert.Single(res);
        Assert.Equal("Foobar Bazqux", res[0].FriendlyName);
        Assert.Equal("Other", res[0].Category);
        Assert.False(res[0].IsFromSeedDictionary);
    }

    [Fact]
    public void Test3_CategoryInference_Known()
    {
        NameMapper.LoadSeedDictionary(_apiMock.Object, _testDir);
        
        var uris = new HashSet<string> { "ms-settings:network-something" };
        var res = NameMapper.MapToEntries(uris);
        
        Assert.Equal("Network & Internet", res[0].Category);
    }

    [Fact]
    public void Test4_CategoryInference_Unknown()
    {
        NameMapper.LoadSeedDictionary(_apiMock.Object, _testDir);
        var uris = new HashSet<string> { "ms-settings:xyzzy" };
        var res = NameMapper.MapToEntries(uris);
        Assert.Equal("Other", res[0].Category);
    }

    [Fact]
    public void Test5_QueryStringStripping()
    {
        NameMapper.LoadSeedDictionary(_apiMock.Object, _testDir);
        var uris = new HashSet<string> { "ms-settings:display?tab=2" };
        var res = NameMapper.MapToEntries(uris);
        Assert.Equal("Display", res[0].FriendlyName);
    }

    [Fact]
    public void Test6_MalformedSeedDictionary()
    {
        File.WriteAllText(Path.Combine(_testDir, "Resources", "friendly_names.json"), "{ invalid json }");
        NameMapper.LoadSeedDictionary(_apiMock.Object, _testDir);
        
        var uris = new HashSet<string> { "ms-settings:display" };
        var res = NameMapper.MapToEntries(uris);
        
        Assert.Single(res);
        Assert.False(res[0].IsFromSeedDictionary);
        Assert.NotEqual("fallback", NameMapper.SeedHash);
        Assert.NotNull(NameMapper.SeedHash);
    }
}
