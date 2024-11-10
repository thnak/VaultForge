using BusinessModels.Base;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

public class FileRaidDataBlockModel : BaseModelEntry
{
    /// <summary>
    /// Liên kết với fileRaidModel
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn thực tế đến data block trên đĩa, Unique
    /// </summary>
    public string AbsolutePath { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreationTime { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ModificationTime { get; set; } = DateTime.Now;

    public long Size { get; set; }

    public FileRaidStatus Status { get; set; }

    public int Index { get; set; }
}

public enum FileRaidStatus
{
    Normal,
    Corrupted,
    Missing,
}