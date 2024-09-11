using System.Linq.Expressions;
using BusinessModels.People;
using BusinessModels.System.FileSystem;

namespace Business.Business.Interfaces.FileSystem;

public interface IFolderSystemBusinessLayer : IBusinessLayerRepository<FolderInfoModel>
{
    /// <summary>
    /// Lấy người dùng theo tên hoặc lấy mặc định Anonymous
    /// </summary>
    /// <param name="username"></param>
    /// <returns>Anonymous when string is empty</returns>
    UserModel? GetUser(string username);

    public FolderInfoModel? Get(string username, string relativePath);
    public List<FolderInfoModel> GetFolderBloodLine(string username, string folderId);
    public FolderInfoModel? GetRoot(string username);

    IAsyncEnumerable<FolderInfoModel> Search(string queryString, string? username, int limit = 10, CancellationToken cancellationTokenSource = default);

    /// <summary>
    ///     Regis new file with auto init absolute path
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="file"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<(bool, string)> CreateFileAsync(FolderInfoModel folder, FileInfoModel file, CancellationToken cancellationToken = default);

    public Task<(bool, string)> CreateFileAsync(string userName, FileInfoModel file, CancellationToken cancellationToken = default);
    public Task<(bool, string)> CreateFolder(string userName, string targetFolderId, string folderName);

    public Task<(bool, string)> CreateFolder(RequestNewFolderModel request);

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);
    public Task<long> GetFolderByteSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);

    /// <summary>
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns>Total folders, total files</returns>
    public Task<(long, long)> GetFolderContentsSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default);
}