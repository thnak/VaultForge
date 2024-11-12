using Microsoft.Extensions.VectorData;

namespace BrainNet.Models.Vector;

public class VectorRecord
{
    [VectorStoreRecordKey] public Guid Index { get; set; } = Guid.NewGuid();
    
    [VectorStoreRecordData] public string Key { get; set; } = string.Empty;
    [VectorStoreRecordData] public string Title { get; set; } = string.Empty;
    [VectorStoreRecordData] public string Description { get; set; } = string.Empty;

    [VectorStoreRecordVector(512, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}