using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedModels;

namespace SettingsAll;

public static class ScoreCalculator
{
    public static bool TryCalculateScore(SettingsEntry entry, string search, string[] filteredTokens, string[] highlightTerms, bool isMultiTerm, Func<string, string, MatchResult> fuzzy,
        out int score, out List<int> highlight)
    {
        score = 0; 
        highlight = null;
        
        if (string.IsNullOrWhiteSpace(search)) return false;

        var fields = new List<(string Text, double Weight)>
        {
            (entry.FriendlyName, 1.0), 
            (entry.Category, 0.5), 
            (entry.UriTokens, 0.3)
        };
        
        if (entry.Aliases != null)
        {
            foreach (var alias in entry.Aliases)
            {
                fields.Add((alias, 0.8));
            }
        }

        // Pass 1: ordered full-string match; bonus only for name/alias
        int pass1 = 0;
        List<int> pass1Highlight = null;
        foreach (var (text, w) in fields)
        {
            if (string.IsNullOrEmpty(text)) continue;
            var r = fuzzy(search, text);
            if (!r.Success || !r.IsSearchPrecisionScoreMet()) continue;
            
            int currentScore = (int)(r.Score * w) + (w >= 0.8 ? 50 : 0);
            if (currentScore > pass1)
            {
                pass1 = currentScore;
                pass1Highlight = w == 1.0 ? r.MatchData : null;
            }
        }

        // Pass 2: every token must match some field (Skipped for single word queries)
        int pass2 = 0;
        if (isMultiTerm && filteredTokens.Length > 0)
        {
            double total = 0; 
            bool ok = true;
            
            foreach (var t in filteredTokens)
            {
                double tb = 0;
                foreach (var (text, w) in fields)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    var r = fuzzy(t, text);
                    if (r.Success && r.IsSearchPrecisionScoreMet()) 
                    {
                        tb = Math.Max(tb, r.Score * w);
                    }
                }
                if (tb == 0) { ok = false; break; }
                total += tb;
            }
            if (ok) pass2 = (int)(total / filteredTokens.Length);
        }

        score = Math.Max(pass1, pass2);
        if (score == 0) return false;
        
        score += entry.IsFromSeedDictionary ? 15 : 0;

        // Highlight: prefer exact pass 1, fallback to merging per-term indices
        if (pass1Highlight != null && pass1 >= pass2)
        {
            highlight = pass1Highlight;
        }
        else if (!string.IsNullOrEmpty(entry.FriendlyName))
        {
            var idx = new SortedSet<int>();
            foreach (var t in highlightTerms)
            {
                var r = fuzzy(t, entry.FriendlyName);
                if (r.Success && r.IsSearchPrecisionScoreMet() && r.MatchData != null) 
                {
                    foreach (var matchIndex in r.MatchData)
                    {
                        idx.Add(matchIndex);
                    }
                }
            }
            highlight = idx.Count > 0 ? idx.ToList() : null;
        }

        return true;
    }
}
