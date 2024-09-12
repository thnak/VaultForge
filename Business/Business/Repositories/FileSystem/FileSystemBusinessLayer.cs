using System.Linq.Expressions;
using Business.Business.Interfaces.FileSystem;
using Business.Data.Interfaces.FileSystem;
using BusinessModels.System.FileSystem;
using MongoDB.Driver;

namespace Business.Business.Repositories.FileSystem;

public class FileSystemBusinessLayer(IFileSystemDatalayer da) : IFileSystemBusinessLayer
{
    public IAsyncEnumerable<FileInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return da.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }


    public IAsyncEnumerable<FileInfoModel> Where(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return da.Where(predicate, cancellationToken, fieldsToFetch);
    }

    public FileInfoModel? Get(string key)
    {
        return da.Get(key);
    }

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> GetAllAsync(CancellationToken cancellationTokenSource)
    {
        return da.GetAllAsync(cancellationTokenSource);
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        return da.CreateAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FileInfoModel> models, CancellationToken cancellationTokenSource = default)
    {
        return da.CreateAsync(models, cancellationTokenSource);
    }

    public Task<(bool, string)> UpdateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        return da.UpdateAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FileInfoModel> models, CancellationToken cancellationToken = default)
    {
        return da.CreateAsync(models, cancellationToken);
    }

    public (bool, string) Delete(string key)
    {
        return da.Delete(key);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        return da.GetFileSize(predicate, cancellationTokenSource);
    }
}