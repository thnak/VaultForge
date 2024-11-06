namespace BrainNet.Models.Result;

public class SearchScore<T>(T value, double score)
{
    public T Value { get; set; } = value;
    public double Score { get; set; } = score;
}