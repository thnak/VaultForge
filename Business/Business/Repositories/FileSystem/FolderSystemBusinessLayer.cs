using System.Linq.Expressions;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.User;
using BusinessModels.System.FileSystem;
using MongoDB.Driver;

namespace Business.Business.Repositories.FileSystem;

public class FolderSystemBusinessLayer(IFileSystemBusinessLayer fileSystemService, IUserBusinessLayer userService) : IFolderSystemBusinessLayer
{
    public IAsyncEnumerable<FolderInfoModel> FindAsync(FilterDefinition<FolderInfoModel> filter, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public FolderInfoModel? Get(string key)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel?> GetAsync(List<string> keys, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FolderInfoModel[], long)> GetAllAsync(int page, int size, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(FolderInfoModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FolderInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(FolderInfoModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FolderInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }

    public FolderInfoModel? GetRoot(string username)
    {
        var user = userService.Get(username);
        if (user == null) return default;
        var folder = Get(user.Folder);
        if (folder == null)
        {
            
        }
        throw new NotImplementedException();
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public long GetFolderSize(Expression<Func<FolderInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }
}