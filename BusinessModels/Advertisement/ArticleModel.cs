using System.Text.Json;
using BusinessModels.Attribute;
using BusinessModels.Base;
using MessagePack;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.Advertisement;

[MessagePackObject]
public class ArticleModel : BaseModelEntry
{
    [JsonDescription("title")] [Key(3)] public string Title { get; set; } = string.Empty;

    [JsonDescription("iso language code")]
    [Key(4)]
    public string Language { get; set; } = string.Empty;

    [JsonDescription("html sheet")]
    [Key(5)]
    public string HtmlSheet { get; set; } = string.Empty;

    [JsonDescription("css style sheet")]
    [Key(6)]
    public string StyleSheet { get; set; } = string.Empty;

    [JsonDescription("javascript sheet")]
    [Key(7)]
    public string JavaScriptSheet { get; set; } = string.Empty;

    [JsonDescription("author")] [Key(8)] public string Author { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [JsonDescription("published date")]
    [Key(9)]
    public DateTime PublishDate { get; set; }


    [JsonDescription("content summary")]
    [Key(10)]
    public string Summary { get; set; } = string.Empty;

    [JsonDescription("image thumbnail link")]
    [Key(11)]
    public string Image { get; set; } = string.Empty;

    [JsonDescription("SEO keywords")]
    [Key(12)]
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