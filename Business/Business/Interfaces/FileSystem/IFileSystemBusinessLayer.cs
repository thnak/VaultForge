using System.Linq.Expressions;
using BusinessModels.System.FileSystem;

namespace Business.Business.Interfaces.FileSystem;

public interface IFileSystemBusinessLayer : IBusinessLayerRepository<FileInfoModel>
{
    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);
    Task<FileInfoModel?> GetRandomFileAsync(string rootFolderId, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch);
    public IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(Expression<Func<FileInfoModel, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch);

}