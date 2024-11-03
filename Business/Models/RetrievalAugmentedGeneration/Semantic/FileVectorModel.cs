using Microsoft.Extensions.VectorData;

namespace Business.Models.RetrievalAugmentedGeneration.Semantic;

public class FileVectorModel
{
    [VectorStoreRecordKey] public int Index { get; set; }

    [VectorStoreRecordData] public string FileId { get; set; } = string.Empty;
    
    [VectorStoreRecordVector(768, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}