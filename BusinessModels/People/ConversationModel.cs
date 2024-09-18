using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.People;

public class ConversationModel
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string UserName { get; set; } = string.Empty;
    public string ConversationName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
}