﻿using System.Linq.Expressions;
using Business.Business.Interfaces.InternetOfThings;
using Business.Data.Interfaces.InternetOfThings;
using Business.Models;
using BusinessModels.General.Results;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Business.Business.Repositories.InternetOfThings;

public class IoTBusinessLayer(IIoTDataLayer data) : IIoTBusinessLayer
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

    public Task<Result<bool>> CreateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        return data.CreateAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(IoTRecord model, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<IoTRecord> updates, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<IoTRecord> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }
}