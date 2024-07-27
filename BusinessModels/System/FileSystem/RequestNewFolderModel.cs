namespace BusinessModels.System.FileSystem;

public class RequestNewFolderModel
{
    public string RootId { get; set; } = string.Empty;
    public string RootPassWord { get; set; } = string.Empty;
    public FolderInfoModel NewFolder { get; set; } = new();
}