using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.InternetOfThings;
using Business.Utils;
using Business.Utils.Protector;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.InternetOfThings;

public class IotSensorDataLayer(IMongoDataLayerContext context, ILogger<IIotSensorDataLayer> logger, IDataProtectionProvider provider, IMemoryCache memoryCache) : IIotSensorDataLayer
{
    private readonly IMongoCollection<IoTSensor> _data = context.MongoDatabase.GetCollection<IoTSensor>(IotSensorCollectionName);
    private readonly IDataProtector _protectionProvider = provider.CreateProtector("IotSensorDataLayerProtector");

    private const string IotSensorCollectionName = "IotSensor";
    
    public void Dispose()
    {
        //
    }

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IndexKeysDefinition<IoTSensor>[] uniqueIndexesDefinitions =
            [
                Builders<IoTSensor>.IndexKeys.Ascending(x => x.DeviceId).Ascending(x => x.SensorId),
            ];
            var uniqueIndexes = uniqueIndexesDefinitions.Select(x => new CreateIndexModel<IoTSensor>(x, new CreateIndexOptions { Unique = true }));
            await _data.Indexes.DropAllAsync(cancellationToken);
            await _data.Indexes.CreateManyAsync(uniqueIndexes, cancellationToken);

            return (true, AppLang.Create_successfully);
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        var result = _data.GetDocumentSizeAsync(cancellationToken: cancellationToken);
        return result;
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<IoTSensor, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = _data.GetDocumentSizeAsync(cancellationToken: cancellationToken);
        return result;
    }

    public IAsyncEnumerable<IoTSensor> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTSensor> FindAsync(FilterDefinition<IoTSensor> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTSensor> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return _data.FindProjectAsync(f => f.SensorId == keyWord, null, cancellationToken);
    }

    public IAsyncEnumerable<IoTSensor> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<IoTSensor, object>>[] fieldsToFetch)
    {
        return _data.FindProjectAsync(f => f.SensorId == keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<IoTSensor> WhereAsync(Expression<Func<IoTSensor, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<IoTSensor, object>>[] fieldsToFetch)
    {
        return _data.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public IoTSensor? Get(string key)
    {
        try
        {
            var cacheKey = IotSensorCollectionName + "Get" + key;
            return memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(1));
                return _data.Get(key) ?? _data.Find(x => x.SensorId == key).FirstOrDefault();
            });
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task<Result<IoTSensor?>> Get(string key, params Expression<Func<IoTSensor, object>>[] fieldsToFetch)
    {
        return _data.Get(key, fieldsToFetch);
    }

    public async IAsyncEnumerable<IoTSensor?> GetAsync(List<string> keys, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        foreach (var m in _data.Get(keys, cancellationToken))
        {
            yield return m;
        }
    }

    public async Task<(IoTSensor[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        var total = await GetDocumentSizeAsync(cancellationToken);

        var result = await _data.FindAsync(FilterDefinition<IoTSensor>.Empty, new FindOptions<IoTSensor, IoTSensor>()
        {
            Skip = page * size,
            Limit = size
        }, cancellationToken);

        List<IoTSensor> devices = await result.ToListAsync(cancellationToken);
        return (devices.ToArray(), total);
    }

    public IAsyncEnumerable<IoTSensor> GetAllAsync(Expression<Func<IoTSensor, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _data.GetAll(field2Fetch, cancellationToken);
    }

    public async Task<Result<bool>> CreateAsync(IoTSensor model, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<IoTSensor>.Filter.Eq(x => x.Id, model.Id);
            var isExist = await _data.Find(filter).AnyAsync(cancellationToken: cancellationToken);
            if (!isExist)
            {
                model.CreateTime = DateTime.UtcNow;
                model.ModifiedTime = DateTime.UtcNow;
                if (string.IsNullOrEmpty(model.SensorId))
                {
                    model.SensorId = model.Id.GenerateAliasKey(DateTime.Now.Ticks.ToString());
                    model.SensorId = _protectionProvider.Protect(model.SensorId);
                }

                await _data.InsertOneAsync(model, cancellationToken: cancellationToken);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(AppLang.File_is_already_exsists, ErrorType.NotFound);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<IoTSensor> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> ReplaceAsync(IoTSensor model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<IoTSensor> updates, CancellationToken cancellationToken = default)
    {
        var sensor = Get(key);
        if (sensor == null)
            return (false, AppLang.Sensor_not_found);
        var updateResult = await _data.UpdateAsync(sensor.Id.ToString(), updates, cancellationToken);
        memoryCache.Remove(IotSensorCollectionName + "Get" + key);
        return (updateResult.IsSuccess, updateResult.Message);
    }

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<IoTSensor> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        try
        {
            if (ObjectId.TryParse(key, out ObjectId id))
            {
                _data.DeleteMany(x => x.Id == id);
                return Task.FromResult((true, AppLang.Delete_successfully));
            }

            _data.DeleteOne(x => x.SensorId == key);
            memoryCache.Remove(IotSensorCollectionName + "Get" + key);
            return Task.FromResult((true, AppLang.Delete_successfully));
        }
        catch (Exception e)
        {
            return Task.FromResult((false, e.Message));
        }
    }
}