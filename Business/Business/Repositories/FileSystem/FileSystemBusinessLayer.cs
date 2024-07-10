using System.Linq.Expressions;
using Business.Business.Interfaces.FileSystem;
using Business.Data.Interfaces.FileSystem;
using BusinessModels.System.FileSystem;
using MongoDB.Driver;

namespace Business.Business.Repositories.FileSystem;

public class FileSystemBusinessLayer(IFileSystemDatalayer da) : IFileSystemBusinessLayer
{
    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public FileInfoModel? Get(string key)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(FileInfoModel model)
    {
        return da.CreateAsync(model);
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FileInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        return da.CreateAsync(models, cancellationTokenSource);
    }

    public Task<(bool, string)> UpdateAsync(FileInfoModel model)
    {
        return da.UpdateAsync(model);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FileInfoModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }
}