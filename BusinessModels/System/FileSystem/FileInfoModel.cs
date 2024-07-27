using System.Text.Json.Serialization;
using BusinessModels.Converter;
using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FileInfoModel
{
    [JsonConverter(typeof(ObjectIdConverter))]
    [BsonId] [Key(0)] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    ///     Dùng cho resource có nhiều biến thể như độ phân giải, chất lượng
    /// </summary>
    [Key(1)]
    public List<FileContents> ExtendResource { get; set; } = [];

    [Key(2)] public string FileName { get; set; } = string.Empty; // Name of the file
    [Key(3)] public string ContentType { get; set; } = string.Empty; // Extension of the file
    [Key(4)] public long FileSize { get; set; } // Size of the file in bytes

    [Key(5)] public List<string> Tags { get; set; } = [];

    [Key(6)] public string MetadataId { get; set; } = string.Empty;

    /// <summary>
    ///     Creation date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(7)]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    ///     Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(8)]
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    ///     Absolute path for file storage
    /// </summary>
    [Key(9)]
    public string AbsolutePath { get; set; } = string.Empty;

    /// <summary>
    ///     Relative path for UI display
    /// </summary>
    [Key(10)]
    public string RelativePath { get; set; } = string.Empty;

    [Key(11)] public string Thumbnail { get; set; } = string.Empty;

    #region Font-End Properties

    [BsonIgnore] [Key(12)] public FileMetadataModel Metadata { get; set; } = new();

    #endregion

    #region Front-End Method

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
        return FileName;
    }

    #endregion
}