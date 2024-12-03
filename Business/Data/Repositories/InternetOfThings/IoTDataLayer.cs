using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.InternetOfThings;
using Business.Models;
using Business.Utils;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WriteConcern = MongoDB.Driver.WriteConcern;

namespace Business.Data.Repositories.InternetOfThings;

public class IoTDataLayer : IIoTDataLayer
{
    public IoTDataLayer(IMongoDataLayerContext context, ILogger<IoTDataLayer> logger)
    {
        try
        {
            var options = new CreateCollectionOptions
            {
                TimeSeriesOptions = new TimeSeriesOptions("timestamp", "deviceId", TimeSeriesGranularity.Seconds)
            };
            context.MongoDatabase.CreateCollection("IotDB", options);
        }
        catch (Exception)
        {
            //
        }

        var writeConcern = new WriteConcern(1, new Optional<TimeSpan?>(TimeSpan.FromSeconds(10)), journal: new Optional<bool?>(false), fsync: false);
        _dataDb = context.MongoDatabase.GetCollection<IoTRecord>("IotDB", new MongoCollectionSettings() { WriteConcern = writeConcern });
        _logger = logger;
    }

    private readonly IMongoCollection<IoTRecord> _dataDb;
    private readonly ILogger<IoTDataLayer> _logger;


    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        IndexKeysDefinition<IoTRecord>[] indexKeysDefinitions = [
            Builders<IoTRecord>.IndexKeys.Descending(x=>x.SensorId).Descending(x => x.Timestamp),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date),
            Builders<IoTRecord>.IndexKeys.Descending(x => x.Date),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Ascending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Descending(x => x.Date).Descending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Descending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Descending(x => x.Date).Ascending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Ascending(x=>x.SensorId).Ascending(x => x.Date).Ascending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Ascending(x=>x.SensorId).Descending(x => x.Date).Ascending(x => x.Hour),
        ];
        
        
        var indexModels = indexKeysDefinitions.Select(x=> new CreateIndexModel<IoTRecord>(x));
        await _dataDb.Indexes.CreateManyAsync(indexModels, cancellationToken);

        return await Task.FromResult((true, string.Empty));
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<IoTRecord, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> FindAsync(FilterDefinition<IoTRecord> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<IoTRecord> WhereAsync(Expression<Func<IoTRecord, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
    {
        var options = fieldsToFetch.Any() ? new FindOptions<IoTRecord, IoTRecord> { Projection = fieldsToFetch.ProjectionBuilder() } : null;
        using var cursor = await _dataDb.FindAsync(predicate, options, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                if (model != default)
                    yield return model;
            }
        }
    }

    public IoTRecord? Get(string key)
    {
        if (ObjectId.TryParse(key, out var objectId))
        {
            return _dataDb.Find(x=>x.Id == objectId).FirstOrDefault();
        }
        return null;
    }

    public async Task<Result<IoTRecord?>> Get(string key, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
    {
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            var findOptions = fieldsToFetch.Any() ? new FindOptions<IoTRecord, IoTRecord>() { Projection = fieldsToFetch.ProjectionBuilder(), Limit = 1 } : null;
            using var cursor = await _dataDb.FindAsync(x => x.Id == objectId, findOptions);
            var article = cursor.FirstOrDefault();
            if (article != null) return Result<IoTRecord?>.Success(article);
            return Result<IoTRecord?>.Failure(AppLang.Article_does_not_exist, ErrorType.NotFound);
        }

        return Result<IoTRecord?>.Failure(AppLang.Invalid_key, ErrorType.Validation);
    }

    public IAsyncEnumerable<IoTRecord?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(IoTRecord[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTRecord> GetAllAsync(Expression<Func<IoTRecord, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _dataDb.GetAll(field2Fetch, cancellationToken);
    }

    public async Task<Result<bool>> CreateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(AppLang.Create_successfully);
        }
        catch (OperationCanceledException e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Cancelled);
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public async Task<Result<bool>> CreateAsync(IReadOnlyCollection<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dataDb.InsertManyAsync(models, cancellationToken: cancellationToken);
            return Result<bool>.Success(AppLang.Create_successfully);
        }
        catch (OperationCanceledException e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Cancelled);
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public Task<(bool, string)> ReplaceAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<IoTRecord> updates, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        //
    }
}