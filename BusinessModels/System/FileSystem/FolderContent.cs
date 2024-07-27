using MessagePack;

namespace BusinessModels.System.FileSystem;

[MessagePackObject]
public class FolderContent
{
    [Key(0)] public string Id { get; set; } = string.Empty;

    [Key(1)] public FolderContentType Type { get; set; }
}

public enum FolderContentType
{
    Folder,
    HiddenFolder,
    DeletedFolder,
    File,
    HiddenFile,
    DeletedFile
}