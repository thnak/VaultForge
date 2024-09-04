using System.Linq.Expressions;
using BusinessModels.System.FileSystem;

namespace Business.Data.Interfaces.FileSystem;

public interface IFileSystemDatalayer : IMongoDataInitializer, IDataLayerRepository<FileInfoModel>
{
    public FileMetadataModel? GetMetaData(string metaId);
    public (bool, string) DeleteMetadata(string metaId);
    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);
}