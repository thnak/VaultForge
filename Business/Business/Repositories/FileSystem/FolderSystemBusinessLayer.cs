using System.Linq.Expressions;
using System.Text;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.User;
using Business.Data.Interfaces.FileSystem;
using BusinessModels.General;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MongoDB.Driver;

namespace Business.Business.Repositories.FileSystem;

public class FolderSystemBusinessLayer(IMemoryCache memoryCache, IFolderSystemDatalayer folderSystemService, IFileSystemBusinessLayer fileSystemService, IUserBusinessLayer userService, IOptions<AppSettings> options) : IFolderSystemBusinessLayer
{
    private readonly string _workingDir = options.Value.FileFolder;

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
        return folderSystemService.Get(key);
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
        return folderSystemService.CreateAsync(model);
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
        if (string.IsNullOrEmpty(username)) username = "Anonymous";
        var absPath = Path.Combine(_workingDir, username);
        var user = userService.Get(absPath);
        if (user == null) return default;
        var folder = Get(user.Folder);
        if (folder == null)
        {
            folder = new FolderInfoModel()
            {
                RelativePath = "/",
                AbsolutePath = absPath,
                FolderName = "Home",
                ModifiedDate = DateTime.UtcNow
            };
            if (!Path.Exists(folder.AbsolutePath)) Directory.CreateDirectory(folder.AbsolutePath);
            var res = CreateAsync(folder).Result;
            if (res.Item1)
            {
                return Get(folder.Id.ToString());
            }

            return default;
        }

        return folder;
    }
    

    public string GetFileMemoryAllocation(FileInfoModel folder)
    {
        StringBuilder stringBuilder = new StringBuilder();
        var userPath = folder.AbsolutePath;
        var time = "_" + DateTime.UtcNow.ToString("yy-MM");
        userPath = Path.Combine(userPath, time);
        stringBuilder.Append(userPath);
        stringBuilder.Append(nameof(Path.Exists));

        bool exists = memoryCache.GetOrCreate(stringBuilder.ToString(), entry => Path.Exists(userPath));

        if (!exists) Directory.CreateDirectory(userPath);
        return userPath;
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