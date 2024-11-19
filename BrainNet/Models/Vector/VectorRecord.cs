using Microsoft.Extensions.VectorData;

namespace BrainNet.Models.Vector;

public class VectorRecord
{
    [VectorStoreRecordKey] public Guid Index { get; set; } = Guid.NewGuid();

    [VectorStoreRecordData(IsFilterable = true)]
    public string Key { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true, IsFullTextSearchable = true)]
    public string Title { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true, IsFullTextSearchable = true)]
    public string Description { get; set; } = string.Empty;

    [VectorStoreRecordVector(512, DistanceFunction.CosineSimilarity, IndexKind.Dynamic)]
    public ReadOnlyMemory<float> Vector { get; set; }
}