using System;
using System.Linq;
using Flow.Launcher.Plugin;

namespace SettingsAll;

public static class ScoreCalculator
{
    public static bool TryCalculateScore(SettingsEntry entry, string search, Func<string, string, int> fuzzySearchScore, out int sortScore, out bool hasHit)
    {
        sortScore = 0;
        hasHit = false;

        int fnScore = fuzzySearchScore(search, entry.FriendlyName);

        int rawAliasScore = 0;
        if (entry.Aliases != null && entry.Aliases.Length > 0)
        {
            rawAliasScore = entry.Aliases.Max(a => fuzzySearchScore(search, a));
        }
        int bestAliasScore = (int)(rawAliasScore * 0.9);

        int categoryScore = fuzzySearchScore(search, entry.Category);

        hasHit = fnScore > 0 || bestAliasScore > 0;

        if (hasHit || categoryScore >= 50)
        {
            int baseScore = Math.Max(fnScore, bestAliasScore);
            int seedBonus = entry.IsFromSeedDictionary ? 20 : -10;
            int categoryBonus = categoryScore > 0 ? 5 : 0;
            
            sortScore = baseScore + seedBonus + categoryBonus;
            return true;
        }

        return false;
    }
}
