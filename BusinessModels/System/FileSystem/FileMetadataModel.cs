using BusinessModels.Base;
using MessagePack;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FileMetadataModel : BaseModelEntry
{
    [Key(3)]
    public TimeSpan Duration { get; set; } // Duration of the media file
    [Key(4)]
    public int? Width { get; set; } // Width of the media (if applicable, e.g., video, image)
    [Key(5)]
    public int? Height { get; set; } // Height of the media (if applicable, e.g., video, image)
    [Key(6)]
    public int? Bitrate { get; set; } // Bitrate of the media file (if applicable, e.g., audio, video)
    [Key(7)]
    public string Codec { get; set; } = string.Empty; // Codec used for the media file
    [Key(8)]
    public string ThumbnailAbsolutePath { get; set; } = string.Empty;

    [Key(9)]
    public DateOnly CreateDate { get; set; }
}