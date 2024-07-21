namespace BusinessModels.System.FileSystem;

[MessagePack.MessagePackObject]
public class FolderContent
{
    [MessagePack.Key(0)]
    public string Id { get; set; } = string.Empty;
    [MessagePack.Key(1)]
    public FolderContentType Type { get; set; }
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