using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.VectorDb;
using Business.Models;
using Business.Models.Vector;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.VectorDb;

public class VectorDataLayer(IMongoDataLayerContext context) : IVectorDataLayer
{
    private readonly IMongoCollection<VectorRecord> _dataDb = context.MongoDatabase.GetCollection<VectorRecord>("Vector");

    public void Dispose()
    {
        //
    }

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        List<IndexKeysDefinition<VectorRecord>> indexKeysDefinitions =
        [
            Builders<VectorRecord>.IndexKeys.Ascending(x => x.Collection).Ascending(x => x.Key)
        ];

        IEnumerable<CreateIndexModel<VectorRecord>> indexesModels = indexKeysDefinitions.Select(x => new CreateIndexModel<VectorRecord>(x));

        await _dataDb.Indexes.DropAllAsync(cancellationToken);
        await _dataDb.Indexes.CreateManyAsync(indexesModels, cancellationToken);
        return (true, string.Empty);
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<VectorRecord, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<VectorRecord> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<VectorRecord> FindAsync(FilterDefinition<VectorRecord> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<VectorRecord> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<VectorRecord> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<VectorRecord, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<VectorRecord> WhereAsync(Expression<Func<VectorRecord, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<VectorRecord, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public VectorRecord? Get(string key)
    {
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            return _dataDb.Find(x => x.Id == objectId).FirstOrDefault();
        }

        return null;
    }

    public Task<Result<VectorRecord?>> Get(string key, params Expression<Func<VectorRecord, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<VectorRecord?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(VectorRecord[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<VectorRecord> GetAllAsync(Expression<Func<VectorRecord, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _dataDb.GetAll(field2Fetch, cancellationToken);
    }

    public async Task<Result<bool>> CreateAsync(VectorRecord model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Failure("canceled", ErrorType.Cancelled);
        }
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<VectorRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> ReplaceAsync(VectorRecord model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<VectorRecord> updates, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<VectorRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    public bool Exists(string collection, string id)
    {
        return _dataDb.Find(x=>x.Collection == collection && x.Key == id).Any();
    }

    public async IAsyncEnumerable<VectorRecord> GetAsyncEnumerator(string collection, string id, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var cursor = await _dataDb.FindAsync(x => x.Collection == collection && x.Key == id, cancellationToken: cancellationToken);
        foreach (VectorRecord record in cursor.ToEnumerable(cancellationToken: cancellationToken))
        {
            yield return record;
        }
    }
}