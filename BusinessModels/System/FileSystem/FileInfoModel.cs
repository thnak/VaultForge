using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

public class FileInfoModel
{
    [BsonId] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    /// <summary>
    /// Dùng cho resource có nhiều biến thể như độ phân giải, chất lượng
    /// </summary>
    public List<string> ExtendResource { get; set; } = [];

    public string FileName { get; set; } = string.Empty; // Name of the file
    public string ContentType { get; set; } = string.Empty; // Extension of the file
    public long FileSize { get; set; } // Size of the file in bytes

    public List<string> Tags { get; set; } = [];

    public string MetadataId { get; set; } = string.Empty;

    /// <summary>
    /// Creation date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Absolute path for file storage
    /// </summary>
    public string AbsolutePath { get; set; } = string.Empty;

    /// <summary>
    /// Relative path for UI display
    /// </summary>
    [BsonIgnore]
    public string RelativePath { get; set; } = string.Empty;

    #region Font-End Properties

    [BsonIgnore] public FileMetadataModel Metadata { get; set; } = new();

    #endregion

    #region Front-End Method

    public override bool Equals(object? o)
    {
        var other = o as FileInfoModel;
        return other?.Id == Id;
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => Id.ToString().GetHashCode();


    public override string ToString()
    {
        return FileName;
    }

    #endregion
}