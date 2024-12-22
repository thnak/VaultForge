using System.Linq.Expressions;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.Interfaces.InternetOfThings;
using Business.Models;
using BusinessModels.General.Results;
using BusinessModels.System.InternetOfThings;
using MongoDB.Driver;

namespace Business.Business.Repositories.InternetOfThings;

public class IoTSensorBusinessLayer(IIotSensorDataLayer iIotSensorDataLayer) : IIoTSensorBusinessLayer
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<IoTSensor, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.GetDocumentSizeAsync(predicate, cancellationToken);
    }

    public IAsyncEnumerable<IoTSensor> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<IoTSensor> FindAsync(FilterDefinition<IoTSensor> filter, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<IoTSensor> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<IoTSensor> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<IoTSensor, object>>[] fieldsToFetch)
    {
        return iIotSensorDataLayer.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<IoTSensor> Where(Expression<Func<IoTSensor, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<IoTSensor, object>>[] fieldsToFetch)
    {
        return iIotSensorDataLayer.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public IoTSensor? Get(string key)
    {
        return iIotSensorDataLayer.Get(key);
    }

    public Task<Result<IoTSensor?>> Get(string key, params Expression<Func<IoTSensor, object>>[] fieldsToFetch)
    {
        return iIotSensorDataLayer.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<IoTSensor?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.GetAsync(keys, cancellationToken);
    }

    public Task<(IoTSensor[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IoTSensor> GetAllAsync(Expression<Func<IoTSensor, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return iIotSensorDataLayer.GetAllAsync(field2Fetch, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IoTSensor model, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<IoTSensor> models, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(IoTSensor model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<IoTSensor> updates, CancellationToken cancellationToken = default)
    {
        return iIotSensorDataLayer.UpdateAsync(key, updates, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<IoTSensor> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        return iIotSensorDataLayer.DeleteAsync(key, cancelToken);
    }
}