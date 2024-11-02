using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Business.Models.RetrievalAugmentedGeneration.Vector;

public class MongoVectorStorageModel
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public float[] Vector { get; set; } = new float[384];
}