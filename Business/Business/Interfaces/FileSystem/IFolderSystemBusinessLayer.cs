using System.Linq.Expressions;
using BrainNet.Models.Result;
using BrainNet.Models.Vector;
using Business.Data.StorageSpace;
using BusinessModels.General.Results;
using BusinessModels.People;
using BusinessModels.System.FileSystem;
using BusinessModels.WebContent.Drive;

namespace Business.Business.Interfaces.FileSystem;

public interface IFolderSystemBusinessLayer : IBusinessLayerRepository<FolderInfoModel>, IExtendService, IDisposable, IAsyncDisposable
{
    Task UpdateFilePropertiesAfterUpload(FileInfoModel file, RedundantArrayOfIndependentDisks.WriteDataResult saveResult, string contentType, string trustedFileNameForDisplay);
    /// <summary>
    /// Lấy người dùng theo tên hoặc lấy mặc định Anonymous
    /// </summary>
    /// <param name="username"></param>
    /// <returns>Anonymous when string is empty</returns>
    UserModel? GetUser(string username);

    public FolderInfoModel? Get(string username, string absoblutePath);
    public List<FolderInfoModel> GetFolderBloodLine(string folderId);
    public FolderInfoModel? GetRoot(string username);
    public IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch);
    public IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(Expression<Func<FolderInfoModel, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch);

    IAsyncEnumerable<FolderInfoModel> Search(string queryString, string? username, int limit = 10, CancellationToken cancellationTokenSource = default);

    /// <summary>
    ///     Regis new file with auto init absolute path
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="file"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<bool>> CreateFileAsync(FolderInfoModel folder, FileInfoModel file, CancellationToken cancellationToken = default);

    public Task<Result<bool>> CreateFileAsync(string userName, FileInfoModel file, CancellationToken cancellationToken = default);
    public Task<Result<bool>> CreateFolder(string userName, string targetFolderId, string folderName);

    public Task<Result<bool>> CreateFolder(RequestNewFolderModel request);

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);
    public Task<long> GetFolderByteSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);

    /// <summary>
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns>Total folders, total files</returns>
    public Task<(long, long)> GetFolderContentsSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);

    public Task<FolderRequest> GetFolderRequestAsync(string folderId,Expression<Func<FolderInfoModel, bool>> folderPredicate, Expression<Func<FileInfoModel, bool>> filePredicate, int pageSize, int pageNumber, bool forceLoad = false, CancellationToken cancellationToken = default);
    public Task<FolderRequest> GetDeletedContentAsync(string? userName, int pageSize, int page, CancellationToken cancellationToken = default);
    public Task<Result<FolderInfoModel?>> InsertMediaContent(string path, CancellationToken cancellationToken = default);

    #region RAG
    public Task RequestIndexAsync(string key, CancellationToken cancellationToken = default);
    public Task<List<SearchScore<VectorRecord>>> SearchRagFromAllDb(string query, int count, CancellationToken cancellationToken = default);
    #endregion

    /// <summary>
    /// decompress file to the same folder
    /// </summary>
    /// <param name="fileId"></param>
    /// <returns></returns>
    public Task<Result<string>> Decompress(string fileId);
}