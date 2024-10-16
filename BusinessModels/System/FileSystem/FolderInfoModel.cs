using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessModels.Converter;
using BusinessModels.General.EnumModel;
using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FolderInfoModel
{
    [JsonConverter(typeof(ObjectIdConverter))]
    [BsonId]
    [Key(0)]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    ///     Name of the folder
    /// </summary>
    [Key(1)]
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    ///     List of content in the folder, include their child folder
    /// </summary>
    [Key(2)]
    [BsonIgnore]
    public List<FolderContent> Contents { get; set; } = [];

    /// <summary>
    ///     The one that own this folder
    /// </summary>
    [Key(3)]
    [BsonElement("Username")]
    public string OwnerUsername { get; set; } = string.Empty;

    [Key(4)] public string ModifiedUserName { get; set; } = string.Empty;

    /// <summary>
    ///     Share this folder to other user
    /// </summary>
    [Key(5)]
    public List<string> SharedTo { get; set; } = [];

    /// <summary>
    ///     Folder password
    /// </summary>
    [Key(6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(7)]
    public DateTime CreateDate { get; set; }

    /// <summary>
    ///     Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(8)]
    public DateTime ModifiedTime { get; set; }

    /// <summary>
    ///     Relative path for UI display, nó có thể thay đổi
    /// </summary>
    [Key(9)]
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Chỉ được tạo 1 lần và không được sửa đổi
    /// </summary>
    [Key(10)]
    public string AbsolutePath { get; set; } = string.Empty;

    [Key(11)] public string VersionId { get; set; } = string.Empty;

    [Key(12)] public FolderContentType Type { get; set; }
    [Key(13)] public FolderContentType PreviousType { get; set; }

    [Key(14)] public string Icon { get; set; } = string.Empty;

    [Key(15)] public string RootFolder { get; set; } = string.Empty;


    #region Front-End Properties

    [BsonIgnore] [Key(16)] public long FolderSize { get; set; }

    #endregion

    #region Front-End Methods

    public FolderInfoModel Copy()
    {
        var textPlan = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<FolderInfoModel>(textPlan)!;
    }

    public override bool Equals(object? o)
    {
        var other = o as FileInfoModel;
        return other?.Id == Id;
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode()
    {
        return Id.ToString().GetHashCode();
    }


    public override string ToString()
    {
        return FolderName;
    }

    #endregion
}