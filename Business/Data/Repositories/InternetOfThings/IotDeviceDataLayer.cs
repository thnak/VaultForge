using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.InternetOfThings;
using Business.Data.Repositories.Utils;
using Business.Utils;
using Business.Utils.Protector;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.InternetOfThings;

public class IotDeviceDataLayer : IIotDeviceDataLayer
{
    private readonly IMongoCollection<IoTDevice> _data;
    private readonly ThreadSafeSearchEngine<IoTDevice> _threadSafeSearchEngine;
    private readonly ILogger<IIotDeviceDataLayer> _logger;
    private const string CollectionName = "IoTDevice";

    public IotDeviceDataLayer(IMongoDataLayerContext context, ILogger<IIotDeviceDataLayer> logger)
    {
        _data = context.MongoDatabase.GetCollection<IoTDevice>(CollectionName);
        _threadSafeSearchEngine = new(CollectionName, SearchEngineExtensions.IoTDeviceDocumentMapper);
        _logger = logger;
    }

    public void Dispose()
    {
        _threadSafeSearchEngine.Dispose();
    }

    public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IndexKeysDefinition<IoTDevice>[] uniqueIndexesDefinitions =
            [
                Builders<IoTDevice>.IndexKeys.Ascending(x => x.DeviceId),
                Builders<IoTDevice>.IndexKeys.Descending(x => x.MacAddress),
                Builders<IoTDevice>.IndexKeys.Ascending(x => x.IpAddress),
            ];
            var uniqueIndexes = uniqueIndexesDefinitions.Select(x => new CreateIndexModel<IoTDevice>(x, new CreateIndexOptions { Unique = true }));
            await _data.Indexes.DropAllAsync(cancellationToken);
            await _data.Indexes.CreateManyAsync(uniqueIndexes, cancellationToken);
            var cursor = GetAllAsync([], cancellationToken: cancellationToken);
            await _threadSafeSearchEngine.LoadAndIndexItems(cursor);
            return Result<bool>.SuccessWithMessage(true, AppLang.Create_successfully);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

    public async Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        var result = await _data.GetDocumentSizeAsync(cancellationToken: cancellationToken);
        return result;
    }

    public async Task<long> GetDocumentSizeAsync(Expression<Func<IoTDevice, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await _data.GetDocumentSizeAsync(cancellationToken: cancellationToken);
        return result;
    }

    public async IAsyncEnumerable<IoTDevice> Search(string queryString, int limit = 10, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        foreach (var item in _threadSafeSearchEngine.Search(queryString, limit))
        {
            yield return item;
        }
    }

    public IAsyncEnumerable<IoTDevice> FindAsync(FilterDefinition<IoTDevice> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTDevice> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return _data.FindProjectAsync(f => f.DeviceId == keyWord, null, cancellationToken);
    }

    public IAsyncEnumerable<IoTDevice> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<IoTDevice, object>>[] fieldsToFetch)
    {
        return _data.FindProjectAsync(f => f.DeviceId == keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<IoTDevice> WhereAsync(Expression<Func<IoTDevice, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<IoTDevice, object>>[] fieldsToFetch)
    {
        return _data.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public IoTDevice? Get(string key)
    {
        try
        {
            return _data.Get(key) ?? _data.Find(x => x.DeviceId == key).FirstOrDefault();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task<Result<IoTDevice?>> Get(string key, params Expression<Func<IoTDevice, object>>[] fieldsToFetch)
    {
        return _data.Get(key, fieldsToFetch);
    }

    public async IAsyncEnumerable<IoTDevice?> GetAsync(List<string> keys, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        foreach (var m in _data.Get(keys, cancellationToken))
        {
            yield return m;
        }
    }

    public async Task<(IoTDevice[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        var total = await GetDocumentSizeAsync(cancellationToken);

        var result = await _data.FindAsync(FilterDefinition<IoTDevice>.Empty, new FindOptions<IoTDevice, IoTDevice>()
        {
            Skip = page * size,
            Limit = size
        }, cancellationToken);

        List<IoTDevice> devices = await result.ToListAsync(cancellationToken);
        return (devices.ToArray(), total);
    }

    public IAsyncEnumerable<IoTDevice> GetAllAsync(Expression<Func<IoTDevice, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _data.GetAll(field2Fetch, cancellationToken);
    }

    public async Task<Result<bool>> CreateAsync(IoTDevice model, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<IoTDevice>.Filter.Eq(x => x.Id, model.Id);
            var isExist = await _data.Find(filter).AnyAsync(cancellationToken: cancellationToken);
            if (!isExist)
            {
                model.CreateTime = DateTime.UtcNow;
                model.ModifiedTime = DateTime.UtcNow;
                if (string.IsNullOrEmpty(model.DeviceId))
                {
                    model.DeviceId = model.Id.GenerateAliasKey(DateTime.Now.Ticks.ToString());
                }

                await _data.InsertOneAsync(model, cancellationToken: cancellationToken);
                _threadSafeSearchEngine.LoadAndIndexItems([model]);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(AppLang.File_is_already_exsists, ErrorType.NotFound);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<IoTDevice> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> ReplaceAsync(IoTDevice model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> UpdateAsync(string key, FieldUpdate<IoTDevice> updates, CancellationToken cancellationToken = default)
    {
        var updateResult = await _data.UpdateAsync(key, updates, cancellationToken);
        return Result<bool>.SuccessWithMessage(updateResult.IsSuccess, updateResult.Message);
    }

    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<IoTDevice> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        bool isSuccess = false;
        var device = Get(key);
        if (device == null)
            return Task.FromResult(Result<bool>.Failure(AppLang.Device_not_found, ErrorType.NotFound));
        try
        {
            if (ObjectId.TryParse(key, out ObjectId id))
            {
                _data.DeleteMany(x => x.Id == id);
                isSuccess = true;
                return Task.FromResult(Result<bool>.SuccessWithMessage(true, AppLang.Delete_successfully));
            }

            _data.DeleteOne(x => x.DeviceId == key);
            isSuccess = true;
            return Task.FromResult(Result<bool>.SuccessWithMessage(true, AppLang.Delete_successfully));
        }
        catch (Exception e)
        {
            return Task.FromResult(Result<bool>.Failure(e.Message, ErrorType.Unknown));
        }
        finally
        {
            if (isSuccess)
            {
                _threadSafeSearchEngine.RemoveItemFromIndex(device);
            }
        }
    }
}