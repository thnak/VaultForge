using BusinessModels.System.FileSystem;

namespace Business.Data.Interfaces.FileSystem;

public interface IFolderSystemDatalayer : IMongoDataInitializer, IDataLayerRepository<FolderInfoModel>
{
    public (FolderInfoModel?, string) GetWithPassword(string id, string password);
    public (bool, string, string) CreateFolder(FolderInfoModel folderInfoModel);
    public string GetParentFolder(string id);
    public FolderResult OpenFolder(string id);
    public (bool, string) ChangeFolderPassword(string id, string password);
}