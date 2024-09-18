using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.People;

[MessagePackObject]
public class ChatWithChatBotMessageModel
{
    [BsonId] [Key(0)] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [Key(1)] public string ConversationId { get; set; } = string.Empty;
    [Key(2)] public string Role { get; set; } = string.Empty;
    [Key(3)] public string Content { get; set; } = string.Empty;
    [Key(4)] public string[] Images { get; set; } = [];
    [Key(5)] public string ToolCalls { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(5)]
    public DateTime Time { get; set; } = DateTime.Now;
}