using BusinessModels.Base;
using BusinessModels.General.EnumModel;
using MessagePack;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FileInfoModel : BaseModelEntry
{
    /// <summary>
    ///     Dùng cho resource có nhiều biến thể như độ phân giải, chất lượng
    /// </summary>
    [Key(3)]
    public string ParentResource { get; set; } = string.Empty;
    [Key(4)] public string FileName { get; set; } = string.Empty; // Name of the file
    [Key(5)] public string ContentType { get; set; } = string.Empty; // Extension of the file
    [Key(6)] public long FileSize { get; set; } // Size of the file in bytes
    [Key(7)] public string TagId { get; set; } = string.Empty;
    [Key(8)] public string MetadataId { get; set; } = string.Empty;
    /// <summary>
    ///     Creation date of the file
    /// </summary>
    [Key(9)]
    public DateOnly CreatedDate { get; set; }
    /// <summary>
    ///     Absolute path for file storage
    /// </summary>
    [Key(10)]
    public string AbsolutePath { get; set; } = string.Empty;
    /// <summary>
    ///     Relative path for UI display
    /// </summary>
    [Key(11)]
    public string RelativePath { get; set; } = string.Empty;
    [Key(12)] public string RootFolder { get; set; } = string.Empty;
    [Key(13)] public FileStatus Status { get; set; }
    [Key(14)] public FileStatus PreviousStatus { get; set; }
    [Key(15)] public FileClassify Classify { get; set; }
    [Key(16)] public string Checksum { get; set; } = string.Empty;
    [Key(17)] public string Description { get; set; } = string.Empty;
    [Key(18)] public string AliasCode { get; set; } = string.Empty;

    #region Front-End Methods

    public override bool Equals(object? o)
    {
        if (o is FileInfoModel other)
        {
            return other.Id == Id;
        }

        return false;
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return FileName;
    }

    #endregion

    // Default constructor for new instances
    public FileInfoModel()
    {
    }

    // Constructor for deserialization
    [BsonConstructor]
    public FileInfoModel(ObjectId id, string absolutePath) : base(id)
    {
        AbsolutePath = absolutePath;
    }
}