using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using BrainNet.Database;
using BrainNet.Models.Result;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using Business.Business.Interfaces.User;
using Business.Data.Interfaces.User;
using Business.Models;
using Business.Models.RetrievalAugmentedGeneration.Vector;
using Business.Services.Configure;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using MongoDB.Driver;

namespace Business.Business.Repositories.User;

public class FaceBusinessLayer(IFaceDataLayer dataLayer, ILogger<FaceBusinessLayer> logger, ApplicationConfiguration applicationConfiguration) : IFaceBusinessLayer
{
    [Experimental("SKEXP0020")] private readonly IVectorDb _vectorDb = new VectorDb(new VectorDbConfig()
    {
        Name = "FaceEmbedding",
        DistantFunc = applicationConfiguration.GetOnnxConfig.FaceEmbeddingModel.DistantFunc,
        VectorSize = applicationConfiguration.GetOnnxConfig.FaceEmbeddingModel.VectorSize
    }, logger);


    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FaceVectorStorageModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(predicate, cancellationToken);
    }

    public IAsyncEnumerable<FaceVectorStorageModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return dataLayer.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<FaceVectorStorageModel> FindAsync(FilterDefinition<FaceVectorStorageModel> filter, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<FaceVectorStorageModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<FaceVectorStorageModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<FaceVectorStorageModel, object>>[] fieldsToFetch)
    {
        return dataLayer.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FaceVectorStorageModel> Where(Expression<Func<FaceVectorStorageModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<FaceVectorStorageModel, object>>[] fieldsToFetch)
    {
        return dataLayer.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public FaceVectorStorageModel? Get(string key)
    {
        return dataLayer.Get(key);
    }

    public Task<Result<FaceVectorStorageModel?>> Get(string key, params Expression<Func<FaceVectorStorageModel, object>>[] fieldsToFetch)
    {
        return dataLayer.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<FaceVectorStorageModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetAsync(keys, cancellationToken);
    }

    public Task<(FaceVectorStorageModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<FaceVectorStorageModel> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FaceVectorStorageModel> GetAllAsync(Expression<Func<FaceVectorStorageModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return dataLayer.GetAllAsync(field2Fetch, cancellationToken);
    }

    [Experimental("SKEXP0020")]
    public async Task<Result<bool>> CreateAsync(FaceVectorStorageModel model, CancellationToken cancellationToken = default)
    {
        await _vectorDb.AddNewRecordAsync(new VectorRecord()
        {
            Vector = model.Vector,
            Key = model.Owner
        }, cancellationToken);
        return await dataLayer.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<FaceVectorStorageModel> models, CancellationToken cancellationToken = default)
    {
        return dataLayer.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(FaceVectorStorageModel model, CancellationToken cancellationToken = default)
    {
        FieldUpdate<FaceVectorStorageModel> update = new FieldUpdate<FaceVectorStorageModel>();
        update.UpdateAllFields(model);
        return dataLayer.UpdateAsync(model.Id.ToString(), update, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FaceVectorStorageModel> updates, CancellationToken cancellationToken = default)
    {
        return dataLayer.UpdateAsync(key, updates, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FaceVectorStorageModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        return dataLayer.DeleteAsync(key, cancelToken);
    }

    [Experimental("SKEXP0020")]
    public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _vectorDb.Init();
        var cursor = GetAllAsync([], cancellationToken);
        await foreach (var item in cursor)
        {
            await _vectorDb.AddNewRecordAsync(new VectorRecord()
            {
                Vector = item.Vector,
                Key = item.Owner
            }, cancellationToken);
        }

        return Result<bool>.SuccessWithMessage(true, AppLang.Success);
    }

    [Experimental("SKEXP0020")]
    public async Task<Result<List<SearchScore<VectorRecord>>>> SearchVectorAsync(float[] vector, CancellationToken cancellationToken = default)
    {
        try
        {
            List<SearchScore<VectorRecord>> result = [];
            var cursor = _vectorDb.Search(new ReadOnlyMemory<float>(vector), 10, cancellationToken);
            await foreach (var search in cursor)
            {
                result.Add(search);
            }

            if (result.Any())
            {
                return Result<List<SearchScore<VectorRecord>>>.Success(result);
            }

            return Result<List<SearchScore<VectorRecord>>>.SuccessWithMessage([], "", ErrorType.NotFound);
        }
        catch (OperationCanceledException e)
        {
            return Result<List<SearchScore<VectorRecord>>>.SuccessWithMessage([], e.Message, ErrorType.Cancelled);
        }
    }

    [Experimental("SKEXP0020")]
    public void Dispose()
    {
        _vectorDb.Dispose();
    }

    [Experimental("SKEXP0020")]
    public ValueTask DisposeAsync()
    {
        return _vectorDb.DisposeAsync();
    }
}