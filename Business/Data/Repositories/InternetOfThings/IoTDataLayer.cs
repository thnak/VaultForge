using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.InternetOfThings;
using Business.Models;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Logging;
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
        var dateIndexKeys = Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date);
        var dateIndexModel = new CreateIndexModel<IoTRecord>(dateIndexKeys);

        var date2HourIndexKeys = Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Ascending(x => x.Hour);
        var date2HourIndexModel = new CreateIndexModel<IoTRecord>(date2HourIndexKeys);
        
        var date2HourTypeIndexKeys = Builders<IoTRecord>.IndexKeys.Ascending(x => x.Date).Ascending(x => x.Hour).Ascending(x=>x.SensorType);
        var date2HourTypeIndexModel = new CreateIndexModel<IoTRecord>(date2HourTypeIndexKeys);

        await _dataDb.Indexes.CreateManyAsync([dateIndexModel, date2HourIndexModel, date2HourTypeIndexModel], cancellationToken);

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

    public IAsyncEnumerable<IoTRecord> Where(Expression<Func<IoTRecord, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IoTRecord? Get(string key)
    {
        throw new NotImplementedException();
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

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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