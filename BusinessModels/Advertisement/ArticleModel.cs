using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessModels.Attribute;
using BusinessModels.Converter;
using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.Advertisement;

[MessagePackObject]
public class ArticleModel
{
    [Key(0)]
    [BsonId]
    [JsonConverter(typeof(ObjectIdConverter))]
    [JsonDescription("article id")]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [Key(1)] [JsonDescription("title")] public string Title { get; set; } = string.Empty;

    [Key(2)]
    [JsonDescription("iso language code")]
    public string Language { get; set; } = string.Empty;

    [Key(3)]
    [JsonDescription("html sheet")]
    public string HtmlSheet { get; set; } = string.Empty;

    [Key(4)]
    [JsonDescription("css style sheet")]
    public string StyleSheet { get; set; } = string.Empty;

    [Key(5)]
    [JsonDescription("javascript sheet")]
    public string JavaScriptSheet { get; set; } = string.Empty;

    [Key(6)] [JsonDescription("author")] public string Author { get; set; } = string.Empty;

    [Key(7)]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [JsonDescription("published date")]
    public DateTime PublishDate { get; set; }

    [Key(8)]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [JsonDescription("modified date")]
    public DateTime ModifiedTime { get; set; }

    [Key(9)]
    [JsonDescription("content summary")]
    public string Summary { get; set; } = string.Empty;

    [Key(10)]
    [JsonDescription("image thumbnail link")]
    public string Image { get; set; } = string.Empty;

    [Key(11)]
    [JsonDescription("SEO keywords")]
    public List<string> Keywords { get; set; } = [];

    #region Methods

    public override string ToString() => Title;

    public ArticleModel Copy()
    {
        var textPlan = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<ArticleModel>(textPlan)!;
    }

    #endregion
}