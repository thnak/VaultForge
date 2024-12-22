using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.ComputeVision;
using Business.Models;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.ComputeVision;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.ComputeVision;

public class YoloLabelDataLayer(IMongoDataLayerContext context) : IYoloLabelDataLayer
{
    private readonly IMongoCollection<YoloLabel> dataContext = context.MongoDatabase.GetCollection<YoloLabel>("YoloLabels");

    public void Dispose()
    {
        //
    }

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        IndexKeysDefinition<YoloLabel>[] indexKeysDefinitions =
        [
            Builders<YoloLabel>.IndexKeys.Ascending(x => x.FileId).Ascending(x => x.Id)
        ];
        var modelIndexes = indexKeysDefinitions.Select(x => new CreateIndexModel<YoloLabel>(x));
        await dataContext.Indexes.CreateManyAsync(modelIndexes, cancellationToken: cancellationToken);
        return (true, string.Empty);
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<YoloLabel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<YoloLabel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<YoloLabel> FindAsync(FilterDefinition<YoloLabel> filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cursor = await dataContext.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public IAsyncEnumerable<YoloLabel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        var filter = Builders<YoloLabel>.Filter.Eq(x => x.FileId, keyWord);
        return FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<YoloLabel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<YoloLabel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<YoloLabel> WhereAsync(Expression<Func<YoloLabel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<YoloLabel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public YoloLabel? Get(string key)
    {
        throw new NotImplementedException();
    }

    public Task<Result<YoloLabel?>> Get(string key, params Expression<Func<YoloLabel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<YoloLabel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(YoloLabel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<YoloLabel> GetAllAsync(Expression<Func<YoloLabel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> CreateAsync(YoloLabel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await dataContext.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<YoloLabel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> ReplaceAsync(YoloLabel model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<YoloLabel> updates, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<YoloLabel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            await dataContext.DeleteManyAsync(x => x.Id == objectId, cancellationToken: cancelToken);
        }
        else
        {
            await dataContext.DeleteManyAsync(x => x.FileId == key, cancellationToken: cancelToken);
        }

        return (true, AppLang.Delete_successfully);
    }
}