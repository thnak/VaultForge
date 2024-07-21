namespace BusinessModels.System.FileSystem;

[MessagePack.MessagePackObject]
public class FileContents
{
    [MessagePack.Key(0)]
    public string Id { get; set; } = string.Empty;
    [MessagePack.Key(1)]
    public FileContentType Type { get; set; }
}

public enum FileContentType
{
    File,
    HiddenFile,
    DeletedFile,
    ThumbnailFile
}