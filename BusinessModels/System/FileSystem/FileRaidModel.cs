using BusinessModels.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

/// <summary>
/// Mỗi đối tượng này ứng với 1 tập tin
/// </summary>
public class FileRaidModel : BaseModelEntry
{
    /// <summary>
    /// Đường dẫn đến model hiện tại, Unique
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreationTime { get; set; } = DateTime.Now;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ModificationTime { get; set; } = DateTime.Now;

    /// <summary>
    /// Kích thước phân tách data block
    /// </summary>
    public int StripSize { get; set; }

    public RaidType RaidType { get; set; }

    public string CheckSum { get; set; } = string.Empty;
    public long Size { get; set; }

    // Default constructor for new instances
    public FileRaidModel()
    {
    }

    // Constructor for deserialization
    [BsonConstructor]
    public FileRaidModel(ObjectId id) : base(id)
    {
    }
}

public enum RaidType
{
    Raid5,
    Raid6,
    Raid0,
    Raid1
}