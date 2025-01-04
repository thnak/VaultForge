using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.InternetOfThings;
using Business.Utils;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using WriteConcern = MongoDB.Driver.WriteConcern;

namespace Business.Data.Repositories.InternetOfThings;

public class IotRecordDataLayer : IIotRecordDataLayer
{
    private const string CollectionName = "IotDB";

    public IotRecordDataLayer(IMongoDataLayerContext context, ILogger<IotRecordDataLayer> logger)
    {
        if (!context.MongoDatabase.ListCollectionNames().ToList().Contains(CollectionName))
        {
            var options = new CreateCollectionOptions
            {
                TimeSeriesOptions = new TimeSeriesOptions(nameof(IoTRecord.CreateTime), nameof(IoTRecord.Metadata), TimeSeriesGranularity.Seconds)
            };
            context.MongoDatabase.CreateCollection(CollectionName, options);
        }


        var writeConcern = new WriteConcern(1, new Optional<TimeSpan?>(TimeSpan.FromSeconds(10)), journal: new Optional<bool?>(false), fsync: false);
        _dataDb = context.MongoDatabase.GetCollection<IoTRecord>(CollectionName, new MongoCollectionSettings() { WriteConcern = writeConcern });
        _logger = logger;
    }

    private readonly IMongoCollection<IoTRecord> _dataDb;
    private readonly ILogger<IotRecordDataLayer> _logger;


    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        IndexKeysDefinition<IoTRecord>[] indexKeysDefinitions =
        [
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Id),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Ascending(x => x.Hour),
            Builders<IoTRecord>.IndexKeys.Ascending(x => x.CreateTime).Ascending(x => x.Metadata.SensorId),
        ];

        await _dataDb.Indexes.DropAllAsync(cancellationToken);
        var indexModels = indexKeysDefinitions.Select(x => new CreateIndexModel<IoTRecord>(x));
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
            return _dataDb.Find(x => x.Id == objectId).FirstOrDefault();
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
            return Result<bool>.SuccessWithMessage(true, AppLang.Create_successfully);
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
            return Result<bool>.SuccessWithMessage(true, AppLang.Create_successfully);
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

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<IoTRecord> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            if (ObjectId.TryParse(key, out var id))
            {
                var filter = Builders<IoTRecord>.Filter.Eq(f => f.Id, id);

                var isExist = await _dataDb.Find(filter).AnyAsync(cancellationToken: cancellationToken);
                if (!isExist)
                    return (false, AppLang.NotFound);

                // Build the update definition by combining multiple updates
                var updateDefinitionBuilder = Builders<IoTRecord>.Update;
                var updateDefinitions = new List<UpdateDefinition<IoTRecord>>();

                if (updates.Any())
                {
                    foreach (var update in updates)
                    {
                        var fieldName = update.Key;
                        var fieldValue = update.Value;

                        // Add the field-specific update to the list
                        updateDefinitions.Add(updateDefinitionBuilder.Set(fieldName, fieldValue));
                    }

                    // Combine all update definitions into one
                    var combinedUpdate = updateDefinitionBuilder.Combine(updateDefinitions);

                    await _dataDb.UpdateManyAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
                }

                return (true, AppLang.Update_successfully);
            }

            return (false, AppLang.Invalid_key);
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty);
        }
    }

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> UpdateIotValue(string key, float value, ProcessStatus processStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            var oldValue = Get(key);
            if (oldValue == null)
                return Result<bool>.Failure(AppLang.NotFound, ErrorType.NotFound);

            var filter = Builders<IoTRecord>.Filter.Eq(record => record.Metadata.SensorId, oldValue.Metadata.SensorId);
            filter &= Builders<IoTRecord>.Filter.Eq(record => record.Metadata.RecordedAt, oldValue.Metadata.RecordedAt);

            var update = Builders<IoTRecord>.Update
                .Set(record => record.Metadata.ProcessStatus, processStatus)
                .Set(record => record.Metadata.SensorData, value);

            var result = await _dataDb.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);

            if (result.MatchedCount == 0)
                return Result<bool>.Failure(AppLang.NotFound, ErrorType.NotFound);

            return Result<bool>.SuccessWithMessage(true, AppLang.Update_successfully);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public void Dispose()
    {
        //
    }
}