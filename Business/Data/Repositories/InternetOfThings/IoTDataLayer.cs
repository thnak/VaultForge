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

    private readonly SemaphoreSlim _semaphore = new(15000, 15000);

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        IndexKeysDefinition<IoTRecord>[] indexKeysDefinitions = [
            Builders<IoTRecord>.IndexKeys.Descending(x=>x.DeviceId).Descending(x => x.Timestamp),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date),
            Builders<IoTRecord>.IndexKeys.Descending(x => x.Date),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Ascending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Descending(x => x.Date).Descending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Descending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Descending(x => x.Date).Ascending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Ascending(x => x.Hour).Ascending(x => x.SensorType),
            Builders<IoTRecord>.IndexKeys.Descending(x => x.Date).Descending(x => x.Hour).Ascending(x => x.SensorType),
            Builders<IoTRecord>.IndexKeys.Ascending(x=>x.DeviceId).Ascending(x => x.Date).Ascending(x => x.Hour).Ascending(x => x.SensorType),
            Builders<IoTRecord>.IndexKeys.Ascending(x=>x.DeviceId).Descending(x => x.Date).Ascending(x => x.Hour).Ascending(x => x.SensorType),
        ];
        
        
        var indexModels = indexKeysDefinitions.Select(x=> new CreateIndexModel<IoTRecord>(x));
        await _dataDb.Indexes.CreateManyAsync(indexModels, cancellationToken);

        return await Task.FromResult((true, string.Empty));
    }

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

    public async IAsyncEnumerable<IoTRecord> Where(Expression<Func<IoTRecord, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
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
        throw new NotImplementedException();
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

    public IAsyncEnumerable<IoTRecord> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> CreateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(AppLang.Create_successfully);
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Result<bool>> CreateAsync(IReadOnlyCollection<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            await _dataDb.InsertManyAsync(models, cancellationToken: cancellationToken);
            return Result<bool>.Success(AppLang.Create_successfully);
        }
        catch (Exception e)
        {
            _logger.LogError(e, null);
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
        finally
        {
            _semaphore.Release();
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
        _semaphore.Dispose();
    }
}