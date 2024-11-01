using Microsoft.Extensions.VectorData;

namespace BrainNet.Models;

public class Movie
{
    [VectorStoreRecordKey]
    public int Key {get;set;}

    [VectorStoreRecordData] public string Title { get; set; } = string.Empty;

    [VectorStoreRecordData] public string Description { get; set; } = string.Empty;

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector {get;set;}
}