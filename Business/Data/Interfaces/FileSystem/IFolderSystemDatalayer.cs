using BusinessModels.System.FileSystem;

namespace Business.Data.Interfaces.FileSystem;

public interface IFolderSystemDatalayer : IMongoDataInitializer, IDataLayerRepository<FolderInfoModel>
{
    
}