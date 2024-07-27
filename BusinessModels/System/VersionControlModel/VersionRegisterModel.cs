using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.VersionControlModel;

public class VersionRegisterModel
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Hash { get; set; } = string.Empty;

    public DateTime Time { get; set; }
}