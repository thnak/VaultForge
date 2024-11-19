using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
using Business.Models;
using Business.Models.RetrievalAugmentedGeneration.Vector;
using Business.Utils;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.User;

public class FaceDataLayer(IMongoDataLayerContext context, ILogger<FaceDataLayer> logger) : IFaceDataLayer
{
    private readonly IMongoCollection<FaceVectorStorageModel> _dataDb = context.MongoDatabase.GetCollection<FaceVectorStorageModel>("FaceVectors");

    public void Dispose()
    {
        //
    }

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        List<IndexKeysDefinition<FaceVectorStorageModel>> indexKeysDefinitions =
        [
            Builders<FaceVectorStorageModel>.IndexKeys.Ascending(x => x.Label),
            Builders<FaceVectorStorageModel>.IndexKeys.Ascending(x => x.Owner),
            Builders<FaceVectorStorageModel>.IndexKeys.Ascending(x => x.Label).Ascending(x => x.Owner)
        ];
        IEnumerable<CreateIndexModel<FaceVectorStorageModel>> indexesModels = indexKeysDefinitions.Select(x => new CreateIndexModel<FaceVectorStorageModel>(x));

        await _dataDb.Indexes.DropAllAsync(cancellationToken);
        await _dataDb.Indexes.CreateManyAsync(indexesModels, cancellationToken);
        return (true, string.Empty);
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FaceVectorStorageModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel> FindAsync(FilterDefinition<FaceVectorStorageModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<FaceVectorStorageModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel> Where(Expression<Func<FaceVectorStorageModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<FaceVectorStorageModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public FaceVectorStorageModel? Get(string key)
    {
        try
        {
            if (ObjectId.TryParse(key, out var objectId))
            {
                return _dataDb.Find(Builders<FaceVectorStorageModel>.Filter.Eq(x => x.Id, objectId)).FirstOrDefault();
            }

            return default;
        }
        catch (MongoException)
        {
            return default;
        }
    }

    public Task<Result<FaceVectorStorageModel?>> Get(string key, params Expression<Func<FaceVectorStorageModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FaceVectorStorageModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel> GetAllAsync(CancellationToken cancellationToken)
    {
        return _dataDb.GetAll(cancellationToken);
    }

    public async Task<Result<bool>> CreateAsync(FaceVectorStorageModel model, CancellationToken cancellationToken = default)
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

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<FaceVectorStorageModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> ReplaceAsync(FaceVectorStorageModel model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FaceVectorStorageModel> updates, CancellationToken cancellationToken = default)
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

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<FaceVectorStorageModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}