using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.Advertisement;

public class ArticleModel
{
    [BsonId] public ObjectId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime PublishDate { get; set; }
    public string Summary { get; set; } = string.Empty;
}