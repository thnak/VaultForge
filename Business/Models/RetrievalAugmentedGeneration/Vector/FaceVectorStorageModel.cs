using BusinessModels.Base;

namespace Business.Models.RetrievalAugmentedGeneration.Vector;

public class FaceVectorStorageModel : BaseModelEntry
{
    public string Label { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } // Timestamp for when the embedding was added
}