using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Business.Models.Vector;

public class VectorModel
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public float[] Vector { get; set; } = new float[384];
}