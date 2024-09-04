using System.Linq.Expressions;
using BusinessModels.System.FileSystem;

namespace Business.Business.Interfaces.FileSystem;

public interface IFileSystemBusinessLayer : IBusinessLayerRepository<FileInfoModel>
{
    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);
}