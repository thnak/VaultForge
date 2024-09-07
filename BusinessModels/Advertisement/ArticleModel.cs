using System.ComponentModel;
using System.Text.Json;
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
    [Description] [Key(3)] public string Content { get; set; } = string.Empty;
    [Key(4)] public string Author { get; set; } = string.Empty;
    [Key(5)] public DateTime PublishDate { get; set; }
    [Key(6)] public DateTime ModifiedDate { get; set; }
    [Key(7)] public string Summary { get; set; } = string.Empty;
    [Key(8)] public List<Dictionary<string, string>> MetaData { get; set; } = new();
    [Key(9)] public string Image { get; set; } = string.Empty;

    #region Methods

    public override string ToString() => Title;

    public ArticleModel Copy()
    {
        var textPlan = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<ArticleModel>(textPlan)!;
    }

    #endregion
}