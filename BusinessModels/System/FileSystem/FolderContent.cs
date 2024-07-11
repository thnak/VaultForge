namespace BusinessModels.System.FileSystem;

public class FolderContent
{
    public string Id { get; set; } = string.Empty;
    public FolderContentType Type { get; set; }
    
    
}

public enum FolderContentType
{
    Folder,
    HiddenFolder,
    File,
    HiddenFile
}