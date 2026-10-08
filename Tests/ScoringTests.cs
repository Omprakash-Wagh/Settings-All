using System.Collections.Generic;
using Xunit;
using Flow.Launcher.Plugin.SharedModels;

namespace SettingsAll.Tests;

public class ScoringTests
{
    private Dictionary<(string, string), MatchResult> _mockScores = new();
    
    private void SetupFuzzy(string query, string target, int score) =>
        _mockScores[(query, target)] =
            new MatchResult(score > 0, SearchPrecisionScore.None, new List<int>(), score);

    private MatchResult MockFuzzySearch(string q, string t) =>
        _mockScores.TryGetValue((q, t), out var r)
            ? r
            : new MatchResult(false, SearchPrecisionScore.None);

    [Fact]
    public void Test1_SeedBonusTiebreak()
    {
        var e1 = new SettingsEntry { FriendlyName = "Name", IsFromSeedDictionary = true };
        var e2 = new SettingsEntry { FriendlyName = "Name", IsFromSeedDictionary = false };
        
        SetupFuzzy("query", "Name", 50);
        
        ScoreCalculator.TryCalculateScore(e1, "query", new string[0], new string[0], false, MockFuzzySearch, out int s1, out _);
        ScoreCalculator.TryCalculateScore(e2, "query", new string[0], new string[0], false, MockFuzzySearch, out int s2, out _);
        
        Assert.True(s1 > s2);
        Assert.Equal(15, s1 - s2);
    }

    [Fact]
    public void Test2_Pass2Fallback()
    {
        var e1 = new SettingsEntry { FriendlyName = "Pass Two", Category = "Cat" };
        
        // Fails Pass 1
        SetupFuzzy("query search", "Pass Two", 0);
        SetupFuzzy("query search", "Cat", 0);
        
        // Succeeds Pass 2 independently
        SetupFuzzy("query", "Pass Two", 50);
        SetupFuzzy("search", "Cat", 40);
        
        var tokens = new[] { "query", "search" };
        bool r = ScoreCalculator.TryCalculateScore(e1, "query search", tokens, tokens, true, MockFuzzySearch, out int score, out _);
        
        Assert.True(r);
        Assert.Equal(35, score); // Average of max weighted token scores: (50*1.0 + 40*0.5)/2 = 35
    }
}
