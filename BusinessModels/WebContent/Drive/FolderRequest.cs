using BusinessModels.System.FileSystem;

namespace BusinessModels.WebContent.Drive;

public class FolderRequest
{
    public FolderInfoModel Folder { get; set; } = new();
    public FileInfoModel[] Files { get; set; } = [];
    public FolderInfoModel[] Folders { get; set; } = [];
    public int TotalFolderPages { get; set; }
    public int TotalFilePages { get; set; }
}