using BusinessModels.System.FileSystem;

namespace BusinessModels.WebContent.Drive;

public class FolderRequest
{
    public FolderInfoModel Folder { get; set; } = new();
    public int TotalPages { get; set; }
}