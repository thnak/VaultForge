using System.Linq.Expressions;
using BusinessModels.System.FileSystem;

namespace Business.Business.Interfaces.FileSystem;

public interface IFolderSystemBusinessLayer : IBusinessLayerRepository<FolderInfoModel>
{
    public FolderInfoModel? Get(string username, string relativePath, bool hashed = true);
    public FolderInfoModel? GetRoot(string username);
    
    /// <summary>
    /// Regis new file with auto init absolute path
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    public (bool, string) CreateFile(FolderInfoModel folder, FileInfoModel file);
    public (bool, string) CreateFile(string userName, FileInfoModel file);

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default);
    public Task<long> GetFolderByteSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns>Total folders, total files</returns>
    public Task<(long, long)> GetFolderContentsSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default);
}