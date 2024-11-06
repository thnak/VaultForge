using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Business.Models.RetrievalAugmentedGeneration.Vector;

public class FaceVectorStorageModel
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public float[] Vector { get; set; } = new float[512];
    public string Label { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
}