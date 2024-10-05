using System.Linq.Expressions;
using System.Text;
using Business.Business.Interfaces.FileSystem;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using Business.Utils.StringExtensions;
using BusinessModels.General.EnumModel;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

namespace Business.Business.Repositories.FileSystem;

public class FileSystemBusinessLayer(IFileSystemDatalayer da, IMemoryCache memoryCache) : IFileSystemBusinessLayer
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FileSystemBusinessLayer));
        stringBuilder.Append(nameof(GetDocumentSizeAsync));
        var cacheKey = stringBuilder.ToString();
        var value = memoryCache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return da.GetDocumentSizeAsync(cancellationToken);
        });
        return value;
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(nameof(FileSystemBusinessLayer));
        stringBuilder.Append(nameof(GetDocumentSizeAsync));
        stringBuilder.Append(predicate.GetCacheKey());
        var cacheKey = stringBuilder.ToString();
        var value = memoryCache.GetOrCreateAsync(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return da.GetDocumentSizeAsync(predicate, cancellationToken);
        });
        return value;
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

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        var file = Get(key);
        if (file == default) return (false, AppLang.File_could_not_be_found);

        if (file.Type != FileContentType.DeletedFile)
        {
            await UpdateAsync(key, new FieldUpdate<FileInfoModel>()
            {
                { model => model.Type, FileContentType.DeletedFile },
                { model => model.PreviousType, file.Type }
            }, cancelToken);
            return (true, AppLang.Delete_successfully);
        }

        return await da.DeleteAsync(key, cancelToken);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationTokenSource = default)
    {
        return da.GetFileSize(predicate, cancellationTokenSource);
    }

    public Task<FileInfoModel?> GetRandomFileAsync(string rootFolderId, CancellationToken cancellationToken = default)
    {
        return da.GetRandomFileAsync(rootFolderId, cancellationToken);
    }

    public IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return da.GetContentFormParentFolderAsync(id, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(Expression<Func<FileInfoModel, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return da.GetContentFormParentFolderAsync(predicate, pageNumber, pageSize, cancellationToken, fieldsToFetch);
    }
}