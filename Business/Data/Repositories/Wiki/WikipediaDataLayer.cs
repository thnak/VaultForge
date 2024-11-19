using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Wiki;
using Business.Models;
using Business.Utils;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.Wiki;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.Wiki;

public class WikipediaDataLayer(IMongoDataLayerContext context, ILogger<WikipediaDataLayer> logger) : IWikipediaDataLayer
{
    private readonly IMongoCollection<WikipediaDatasetModel> _dataDb = context.MongoDatabase.GetCollection<WikipediaDatasetModel>("Wikipedia");

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        List<IndexKeysDefinition<WikipediaDatasetModel>> indexKeysDefinitions =
        [
            Builders<WikipediaDatasetModel>.IndexKeys.Ascending(x => x.Title),
            Builders<WikipediaDatasetModel>.IndexKeys.Ascending(x => x.Title),
            Builders<WikipediaDatasetModel>.IndexKeys.Ascending(x => x.Url)
        ];
        IEnumerable<CreateIndexModel<WikipediaDatasetModel>> indexesModels = indexKeysDefinitions.Select(x => new CreateIndexModel<WikipediaDatasetModel>(x));

        await _dataDb.Indexes.DropAllAsync(cancellationToken);
        await _dataDb.Indexes.CreateManyAsync(indexesModels, cancellationToken);
        return (true, string.Empty);
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return _dataDb.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<WikipediaDatasetModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _dataDb.EstimatedDocumentCountAsync(new EstimatedDocumentCountOptions() { }, cancellationToken);
    }

    public IAsyncEnumerable<WikipediaDatasetModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> FindAsync(FilterDefinition<WikipediaDatasetModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<WikipediaDatasetModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> Where(Expression<Func<WikipediaDatasetModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<WikipediaDatasetModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public WikipediaDatasetModel? Get(string key)
    {
        if (ObjectId.TryParse(key, out ObjectId id))
        {
            return _dataDb.Find(x => x.Id == id).FirstOrDefault();
        }

        return null;
    }

    public Task<Result<WikipediaDatasetModel?>> Get(string key, params Expression<Func<WikipediaDatasetModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(WikipediaDatasetModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> CreateAsync(WikipediaDatasetModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var isExists = await _dataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
            if (isExists) return Result<bool>.Failure(AppLang.Folder_already_exists, ErrorType.Duplicate);
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Create] Operation cancelled");
            return Result<bool>.Failure("canceled", ErrorType.Cancelled);
        }
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<WikipediaDatasetModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> ReplaceAsync(WikipediaDatasetModel model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<WikipediaDatasetModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dataDb.UpdateAsync(key, updates, cancellationToken: cancellationToken);
            if (result.IsSuccess)
                return (true, AppLang.Update_successfully);
            return (false, AppLang.User_update_failed);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return (false, string.Empty);
        }
    }

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<WikipediaDatasetModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}