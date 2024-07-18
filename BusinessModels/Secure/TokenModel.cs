using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.Secure;

[MessagePackObject]
public class TokenModel
{
    [Key(0)]
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();


    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(1)]
    public DateTime ExpireTime { get; set; }

    [Key(2)]
    public string CreateByUser { get; set; } = string.Empty;

    [Key(3)]
    public string Token { get; set; } = string.Empty;
    
    
}