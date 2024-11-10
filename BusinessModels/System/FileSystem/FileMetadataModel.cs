using BusinessModels.Base;
using MessagePack;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FileMetadataModel : BaseModelEntry
{
    [Key(1)] public TimeSpan Duration { get; set; } // Duration of the media file
    [Key(2)] public int? Width { get; set; } // Width of the media (if applicable, e.g., video, image)
    [Key(3)] public int? Height { get; set; } // Height of the media (if applicable, e.g., video, image)
    [Key(4)] public int? Bitrate { get; set; } // Bitrate of the media file (if applicable, e.g., audio, video)
    [Key(5)] public string Codec { get; set; } = string.Empty; // Codec used for the media file
    [Key(6)] public string ThumbnailAbsolutePath { get; set; } = string.Empty;
    [Key(7)] public string ThumbnailContentType { get; set; } = string.Empty;

    [Key(8)]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreateDate { get; set; }
}