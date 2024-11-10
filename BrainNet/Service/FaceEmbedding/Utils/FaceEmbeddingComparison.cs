namespace BrainNet.Service.FaceEmbedding.Utils;

public static class FaceEmbeddingComparison
{
    public static double CosineSimilarity(this float[] vectorA, float[] vectorB)
    {
        // Calculate the dot product
        double dotProduct = vectorA.Zip(vectorB, (a, b) => a * b).Sum();

        // Calculate the magnitudes
        double magnitudeA = Math.Sqrt(vectorA.Sum(a => a * a));
        double magnitudeB = Math.Sqrt(vectorB.Sum(b => b * b));

        // Calculate cosine similarity
        return dotProduct / (magnitudeA * magnitudeB);
    }
}