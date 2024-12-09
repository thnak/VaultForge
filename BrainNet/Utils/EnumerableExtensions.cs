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

    /// <summary>
    /// Groups a list of <see cref="SearchScore{string}"/> by their values and returns a list containing
    /// the first instance from each group, ordered by the frequency of the values in descending order.
    /// </summary>
    /// <param name="searchScores">The list of <see cref="SearchScore{string}"/> to group.</param>
    /// <returns>
    /// A list of <see cref="SearchScore{string}"/> containing one instance per unique value, 
    /// ordered by the number of occurrences in descending order.
    /// </returns>
    public static List<SearchScore<string>> GroupBySearchScore(this List<SearchScore<string>> searchScores)
    {
        var result = searchScores
            .GroupBy(ss => ss.Value)
            .OrderByDescending(g => g.Count())
            .Select(x => x.First()).ToList();
        return result;
    }

    public static List<SearchScore<VectorRecord>> GroupBySearchScore(this List<SearchScore<VectorRecord>> searchScores)
    {
        var result = searchScores
            .GroupBy(ss => ss.Value.Key)
            .OrderByDescending(g => g.Count())
            .Select(x => x.First()).ToList();
        return result;
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

    public static string GetMostFrequentlySearchedKey(this List<SearchScore<VectorRecord>> searchScores)
    {
        if (searchScores.Count == 0)
            return string.Empty; // Return empty if the list is null or empty.

        // Group by the Value property and count occurrences
        var mostSearched = searchScores
            .GroupBy(ss => ss.Value.Key)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return mostSearched ?? string.Empty; // Return empty if no key is found.
    }
}