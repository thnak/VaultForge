using BrainNet.Models.Result;
using BrainNet.Models.Vector;

namespace BrainNet.Utils;

public static class EnumerableExtensions
{
    public static bool IsMatchingMostSearchedValue(this List<SearchScore<string>> searchScores, string key)
    {
        if (searchScores.Count == 0)
            return false;

        // Group by the Value property and count occurrences
        var mostSearched = searchScores
            .GroupBy(ss => ss.Value)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        // Check if the given key matches the most searched value
        return mostSearched != null && mostSearched.Equals(key, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMatchingMostSearchedValue(this List<SearchScore<VectorRecord>> searchScores, string key)
    {
        if (searchScores.Count == 0)
            return false;

        // Group by the Value property and count occurrences
        var mostSearched = searchScores
            .GroupBy(ss => ss.Value.Key)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        // Check if the given key matches the most searched value
        return mostSearched != null && mostSearched.Equals(key, StringComparison.OrdinalIgnoreCase);
    }
}