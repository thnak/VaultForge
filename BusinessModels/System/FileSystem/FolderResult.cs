namespace BusinessModels.System.FileSystem;

public class FolderResult
{
    public List<FolderInfoModel> Folders { get; set; } = [];
    public List<FileInfoModel> Files { get; set; } = [];
}