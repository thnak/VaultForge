using BusinessModels.Base;
using BusinessModels.General.EnumModel;
using MessagePack;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FileInfoModel : BaseModelEntry
{
    /// <summary>
    ///     Dùng cho resource có nhiều biến thể như độ phân giải, chất lượng
    /// </summary>
    [Key(1)]
    public string ParentResource { get; set; } = string.Empty;

    [Key(2)] public string FileName { get; set; } = string.Empty; // Name of the file

    [Key(3)] public string ContentType { get; set; } = string.Empty; // Extension of the file

    [Key(4)] public long FileSize { get; set; } // Size of the file in bytes

    [Key(5)] public string TagId { get; set; } = string.Empty;
    
    [Key(6)] public string MetadataId { get; set; } = string.Empty;

    /// <summary>
    ///     Creation date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc, DateOnly = true)]
    [Key(7)]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    ///     Last modified date of the file
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(8)]
    public DateTime ModifiedTime { get; set; }

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

    [Key(11)] public string RootFolder { get; set; } = string.Empty;


    [Key(13)] public FileStatus Status { get; set; }

    [Key(14)] public FileStatus PreviousStatus { get; set; }

    [Key(15)] public FileClassify Classify { get; set; }

    [Key(16)] public string Checksum { get; set; } = string.Empty;

    [BsonIgnore]
    private DateTime _createTime;

    /// <summary>
    ///     Create time of the file entry
    /// </summary>
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [Key(17)]
    public DateTime CreateTime
    {
        get => _createTime;
        set
        {
            _createTime = value;
            CreatedDate = _createTime.Date;
        }
    }


    [Key(18)] public float[] Vector { get; set; } = [];
    [Key(19)] public string Description { get; set; } = string.Empty;
    [Key(20)] public string AliasCode { get; set; } = string.Empty;

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
}