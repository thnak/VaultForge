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
    public List<FolderContent> Contents { get; set; } = [];

    /// <summary>
    ///     The one that own this folder
    /// </summary>
    [Key(3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    ///     Share this folder to other user
    /// </summary>
    [Key(4)]
    public List<string> SharedTo { get; set; } = [];

    /// <summary>
    ///     Folder password
    /// </summary>
    [Key(5)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    ///     Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(6)]
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    ///     Relative path for UI display
    /// </summary>
    [Key(7)]
    public string RelativePath { get; set; } = string.Empty;

    [Key(8)] public string VersionId { get; set; } = string.Empty;

    [Key(9)] public FolderContentType Type { get; set; }

    [Key(10)] public string Icon { get; set; } = string.Empty;

    [Key(11)] public string RootFolder { get; set; } = string.Empty;


    #region Front-End Properties

    [BsonIgnore] [Key(12)] public long FolderSize { get; set; }

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