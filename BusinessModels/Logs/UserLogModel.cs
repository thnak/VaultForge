using MessagePack;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BusinessModels.Logs
{
    [MessagePackObject]
    public class UserLogModel
    {
        [BsonId]
        [Key(0)]
        public ObjectId ObjectId { get; set; } = ObjectId.GenerateNewId();
        [Key(1)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Key(2)]
        public int Time { get; set; }


        [Key(3)]
        public string UserId { get; set; } = string.Empty;


        [Key(4)]
        public string ObjectName { get; set; } = string.Empty;


        [Key(5)]
        public string Classify { get; set; } = string.Empty;

        [Key(6)]
        public string Action { get; set; } = string.Empty;

        [Key(7)]
        public string OldValue { get; set; } = string.Empty;

        [Key(8)]
        public string NewValue { get; set; } = string.Empty;

        [Key(9)]
        public string Note { get; set; } = string.Empty;
    }
}
