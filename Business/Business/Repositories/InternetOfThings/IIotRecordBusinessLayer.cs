﻿using System.Linq.Expressions;
using BrainNet.Models.Result;
using BrainNet.Models.Vector;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.Interfaces.InternetOfThings;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;
using MongoDB.Driver;

namespace Business.Business.Repositories.InternetOfThings;

public class IotRecordBusinessLayer(IIotRecordDataLayer data, IIotRequestQueue iotRequestQueue) : IIotRecordBusinessLayer
{
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
        return data.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public IoTRecord? Get(string key)
    {
        return data.Get(key);
    }

    public Task<Result<IoTRecord?>> Get(string key, params Expression<Func<IoTRecord, object>>[] fieldsToFetch)
    {
        return data.Get(key, fieldsToFetch);
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
        return data.GetAllAsync(field2Fetch, cancellationToken);
    }

    public IAsyncEnumerable<IoTRecord> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> CreateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        return data.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        return data.CreateAsync(models, cancellationToken);
    }

    public Task<Result<bool>> UpdateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> UpdateAsync(string key, FieldUpdate<IoTRecord> updates, CancellationToken cancellationToken = default)
    {
        return data.UpdateAsync(key, updates, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var cursor = Where(x => x.CreateTime >= today, cancellationToken);
        await foreach (var item in cursor)
        {
            iotRequestQueue.IncrementTotalRequests(item.Metadata.SensorId);
        }

        var cursor2 = Where(x => x.Metadata.ProcessStatus != ProcessStatus.Completed && x.Metadata.ProcessStatus != ProcessStatus.Failed, cancellationToken);
        await foreach (var item in cursor2)
        {
            await iotRequestQueue.QueueRequest(item, cancellationToken);
        }

        return Result<bool>.SuccessWithMessage(true, AppLang.Success);
    }

    public Task<Result<List<SearchScore<VectorRecord>>>> SearchVectorAsync(float[] vector, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> UpdateIotValue(string key, float value, ProcessStatus processStatus, CancellationToken cancellationToken = default)
    {
        return data.UpdateIotValue(key, value, processStatus, cancellationToken);
    }

    public Task<Result<bool>> UpdateIoTValuesBatch(IEnumerable<IoTRecordUpdateModel> updates, CancellationToken cancellationToken = default)
    {
        return data.UpdateIoTValuesBatch(updates, cancellationToken);
    }
}