using MongoDB.Bson;

namespace Business.Models.Authenticate;

public class DataProtectionKey
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string FriendlyName { get; set; } = string.Empty;
    public string Xml { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; } = DateTime.UtcNow;
}