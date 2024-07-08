using System.Linq.Expressions;
using BusinessModels.System.FileSystem;

namespace Business.Business.Interfaces.FileSystem;

public interface IFolderSystemBusinessLayer : IBusinessLayerRepository<FolderInfoModel>
{
    public FolderInfoModel? GetRoot(string username);
    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default);
    public long GetFolderSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default);
    
    
}