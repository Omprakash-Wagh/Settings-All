using System.Collections.Generic;
using Xunit;

namespace SettingsAll.Tests;

public class ScoringTests
{
    private Dictionary<(string, string), int> _mockScores = new();
    
    private void SetupFuzzy(string query, string target, int score)
    {
        _mockScores[(query, target)] = score;
    }

    private int MockFuzzySearch(string query, string target)
    {
        return _mockScores.TryGetValue((query, target), out int score) ? score : 0;
    }

    [Fact]
    public void Test1_TierOrderingGuarantee()
    {
        var entry1 = new SettingsEntry { FriendlyName = "WeakName", Category = "Cat", IsFromSeedDictionary = true };
        SetupFuzzy("query", "WeakName", 3);
        SetupFuzzy("query", "Cat", 0);
        
        bool r1 = ScoreCalculator.TryCalculateScore(entry1, "query", MockFuzzySearch, out int s1, out bool h1);
        Assert.True(r1);
        Assert.True(h1);

        var entry2 = new SettingsEntry { FriendlyName = "NoMatch", Category = "StrongCat", IsFromSeedDictionary = false };
        SetupFuzzy("query", "NoMatch", 0);
        SetupFuzzy("query", "StrongCat", 80);

        bool r2 = ScoreCalculator.TryCalculateScore(entry2, "query", MockFuzzySearch, out int s2, out bool h2);
        Assert.True(r2);
        Assert.False(h2);
    }

    [Fact]
    public void Test2_SeedBonusTiebreak()
    {
        var e1 = new SettingsEntry { FriendlyName = "Name", Category = "Cat", IsFromSeedDictionary = true };
        var e2 = new SettingsEntry { FriendlyName = "Name", Category = "Cat", IsFromSeedDictionary = false };
        
        SetupFuzzy("query", "Name", 50);
        SetupFuzzy("query", "Cat", 0);
        
        ScoreCalculator.TryCalculateScore(e1, "query", MockFuzzySearch, out int s1, out _);
        ScoreCalculator.TryCalculateScore(e2, "query", MockFuzzySearch, out int s2, out _);
        
        Assert.True(s1 > s2);
        Assert.Equal(30, s1 - s2);
    }

    [Fact]
    public void Test3_AliasScaling()
    {
        var e1 = new SettingsEntry { FriendlyName = "No", Category = "Cat", Aliases = new[] { "Yes" } };
        SetupFuzzy("query", "No", 0);
        SetupFuzzy("query", "Cat", 0);
        SetupFuzzy("query", "Yes", 100);
        
        ScoreCalculator.TryCalculateScore(e1, "query", MockFuzzySearch, out int s1, out _);
        Assert.Equal(80, s1);
    }

    [Fact]
    public void Test4_CategoryOnlyThreshold()
    {
        var e1 = new SettingsEntry { FriendlyName = "No", Category = "Cat" };
        SetupFuzzy("query", "No", 0);
        
        SetupFuzzy("query", "Cat", 49);
        bool included1 = ScoreCalculator.TryCalculateScore(e1, "query", MockFuzzySearch, out _, out _);
        Assert.False(included1);

        SetupFuzzy("query", "Cat", 50);
        bool included2 = ScoreCalculator.TryCalculateScore(e1, "query", MockFuzzySearch, out _, out _);
        Assert.True(included2);
    }

    [Fact]
    public void Test5_NoFalseExclusions()
    {
        var e1 = new SettingsEntry { FriendlyName = "Weak", Category = "Cat" };
        SetupFuzzy("query", "Weak", 1);
        SetupFuzzy("query", "Cat", 0);
        
        bool included = ScoreCalculator.TryCalculateScore(e1, "query", MockFuzzySearch, out int score, out _);
        Assert.True(included);
        Assert.Equal(-9, score);
    }
}
