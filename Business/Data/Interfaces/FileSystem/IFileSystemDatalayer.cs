using BusinessModels.System.FileSystem;

namespace Business.Data.Interfaces.FileSystem;

public interface IFileSystemDatalayer : IMongoDataInitializer, IDataLayerRepository<FileInfoModel>
{
    
}