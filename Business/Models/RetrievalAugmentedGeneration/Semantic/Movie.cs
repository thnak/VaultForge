using Microsoft.Extensions.VectorData;
using MongoDB.Bson;

namespace Business.Models.RetrievalAugmentedGeneration.Semantic;

public class Movie
{
    [VectorStoreRecordKey]
    public int Key {get;set;}

    [VectorStoreRecordData] public string Title { get; set; } = string.Empty;

    [VectorStoreRecordData] public string Description { get; set; } = string.Empty;

    [VectorStoreRecordVector(768, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector {get;set;}
}