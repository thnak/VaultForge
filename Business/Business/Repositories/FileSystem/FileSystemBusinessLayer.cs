using System.Linq.Expressions;
using Business.Business.Interfaces.FileSystem;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using BusinessModels.System.FileSystem;
using MongoDB.Driver;

namespace Business.Business.Repositories.FileSystem;

public class FileSystemBusinessLayer(IFileSystemDatalayer da) : IFileSystemBusinessLayer
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return da.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return da.GetDocumentSizeAsync(predicate, cancellationToken);
    }

    public IAsyncEnumerable<FileInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return da.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationToken cancellationToken = default)
    {
        return da.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return da.FindAsync(keyWord, cancellationToken);
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

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return da.GetAsync(keys, cancellationToken);
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return da.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<FileInfoModel> GetAllAsync(CancellationToken cancellationToken)
    {
        return da.GetAllAsync(cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FileInfoModel> updates, CancellationToken cancellationToken = default)
    {
        return da.UpdateAsync(key, updates, cancellationToken);
    }

    public Task<(bool, string)> CreateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        return da.CreateAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FileInfoModel> models, CancellationToken cancellationToken = default)
    {
        return da.CreateAsync(models, cancellationToken);
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

    public Task<FileInfoModel?> GetRandomFileAsync(string rootFolderId, CancellationToken cancellationToken = default)
    {
        return da.GetRandomFileAsync(rootFolderId, cancellationToken);
    }

    public  IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return da.GetContentFormParentFolderAsync(id, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(Expression<Func<FileInfoModel, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return da.GetContentFormParentFolderAsync(predicate, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }
}