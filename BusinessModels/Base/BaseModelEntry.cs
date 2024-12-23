using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using BusinessModels.Converter;
using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.Base;

[MessagePackObject]
public class BaseModelEntry
{
    [BsonId]
    [Key(0)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonConverter(typeof(ObjectIdConverter))]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(1)]
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(2)]
    public DateTime ModifiedTime { get; set; } = DateTime.UtcNow;
}