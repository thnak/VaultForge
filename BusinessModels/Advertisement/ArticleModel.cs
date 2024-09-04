using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.Advertisement;

[MessagePackObject]
public class ArticleModel
{
    [Key(0)] [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [Key(1)] public string Title { get; set; } = string.Empty;
    [Key(2)] public string Language { get; set; } = string.Empty;
    [Key(3)] public string Content { get; set; } = string.Empty;
    [Key(4)] public string Author { get; set; } = string.Empty;
    [Key(5)] public DateTime PublishDate { get; set; }
    [Key(6)] public string Summary { get; set; } = string.Empty;


    #region Methods

    public override string ToString() => Title;

    #endregion
}