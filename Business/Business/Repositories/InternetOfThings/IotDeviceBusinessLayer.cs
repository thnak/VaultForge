﻿using System.Linq.Expressions;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.Interfaces.InternetOfThings;
using Business.Models;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using MongoDB.Driver;

namespace Business.Business.Repositories.InternetOfThings;

public class IotDeviceBusinessLayer(IIotDeviceDataLayer dataLayer, IIotSensorDataLayer iIotSensorDataLayer) : IIotDeviceBusinessLayer
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<IoTDevice, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(predicate, cancellationToken);
    }

    public IAsyncEnumerable<IoTDevice> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return dataLayer.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<IoTDevice> FindAsync(FilterDefinition<IoTDevice> filter, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<IoTDevice> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<IoTDevice> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<IoTDevice, object>>[] fieldsToFetch)
    {
        return dataLayer.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<IoTDevice> Where(Expression<Func<IoTDevice, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<IoTDevice, object>>[] fieldsToFetch)
    {
        return dataLayer.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public IoTDevice? Get(string key)
    {
        return dataLayer.Get(key);
    }

    public Task<Result<IoTDevice?>> Get(string key, params Expression<Func<IoTDevice, object>>[] fieldsToFetch)
    {
        return dataLayer.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<IoTDevice?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetAsync(keys, cancellationToken);
    }

    public Task<(IoTDevice[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<IoTDevice> GetAllAsync(Expression<Func<IoTDevice, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return dataLayer.GetAllAsync(field2Fetch, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IoTDevice model, CancellationToken cancellationToken = default)
    {
        return dataLayer.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<IoTDevice> models, CancellationToken cancellationToken = default)
    {
        return dataLayer.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(IoTDevice model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<IoTDevice> updates, CancellationToken cancellationToken = default)
    {
        return dataLayer.UpdateAsync(key, updates, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<IoTDevice> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        var device = Get(key);
        if (device == null)
            return (false, AppLang.Device_not_found);

        var result = await dataLayer.DeleteAsync(key, cancelToken).ConfigureAwait(false);
        if (result.Item1)
        {
            var deviceId = device.Id.ToString(); 
            var sensors = iIotSensorDataLayer.WhereAsync(x => x.DeviceId == deviceId, cancelToken);
            await foreach (var sensor in sensors)
            {
                await iIotSensorDataLayer.DeleteAsync(sensor.Id.ToString(), cancelToken);
            }
        }

        return result;
    }
}