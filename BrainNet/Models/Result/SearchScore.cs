namespace BrainNet.Models.Result;

public class SearchScore<T>(T value, double score)
{
    public T Value { get; set; } = value;
    public double Score { get; set; } = score;
}

public class SearchScorer<T>
{
    public Dictionary<TKey, double> GetClassScores<TKey>(
        List<SearchScore<T>> results,
        Func<T, TKey> classSelector,
        double alpha = 1.0,
        double beta = 0.5,
        double threshold = 0) where TKey : notnull
    {
        return results
            .Where(r => r.Score > threshold) // Filter only positive scores
            .GroupBy(r => classSelector(r.Value)) // Group by class
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    double sumScores = g.Sum(r => r.Score); // Total score for the class
                    int count = g.Count(); // Number of matches in the class
                    return alpha * sumScores + beta * count; // Weighted combination
                });
    }

    public Dictionary<TKey, double> GetClassScoresWithAverages<TKey>(
        List<SearchScore<T>> results,
        Func<T, TKey> classSelector,
        double alpha = 1.0,
        double beta = 0.5,
        double threshold = 0) where TKey : notnull
    {
        return results
            .Where(r => r.Score > threshold)
            .GroupBy(r => classSelector(r.Value))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    double avgScore = g.Average(r => r.Score); // Average score for the class
                    int count = g.Count(); // Number of matches in the class
                    return alpha * avgScore + beta * count; // Weighted combination
                });
    }

    public Dictionary<TKey, double> GetWeightedTopScores<TKey>(
        List<SearchScore<T>> results,
        Func<T, TKey> classSelector,
        double decayFactor = 0.8,
        double threshold = 0) where TKey : notnull
    {
        return results
            .Where(r => r.Score > threshold)
            .GroupBy(r => classSelector(r.Value))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var sortedScores = g.OrderByDescending(r => r.Score).ToList();
                    double weightedSum = 0.0;

                    for (int i = 0; i < sortedScores.Count; i++)
                    {
                        weightedSum += sortedScores[i].Score * Math.Pow(decayFactor, i);
                    }

                    return weightedSum;
                }).OrderByDescending(x => x.Value).ToDictionary();
    }


    public TKey GetBestClass<TKey>(
        List<SearchScore<T>> results,
        Func<T, TKey> classSelector,
        double alpha = 1.0,
        double beta = 0.5) where TKey : notnull
    {
        var classScores = GetClassScores(results, classSelector, alpha, beta);
        return classScores.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
    }
}