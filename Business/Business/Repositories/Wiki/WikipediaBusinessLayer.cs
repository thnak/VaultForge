using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using BrainNet.Database;
using BrainNet.Models.Result;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using Business.Business.Interfaces.Wiki;
using Business.Business.Utils;
using Business.Data.Interfaces.VectorDb;
using Business.Data.Interfaces.Wiki;
using Business.Models;
using Business.Services.Configure;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Business.Utils.StringExtensions;
using BusinessModels.General.Results;
using BusinessModels.Wiki;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using MongoDB.Driver;

namespace Business.Business.Repositories.Wiki;

public class WikipediaBusinessLayer(
    IWikipediaDataLayer dataLayer,
    IVectorDataLayer vectorDataLayer,
    ILogger<WikipediaBusinessLayer> logger,
    IParallelBackgroundTaskQueue parallelBackgroundTaskQueue,
    ApplicationConfiguration applicationConfiguration) : IWikipediaBusinessLayer
{
    [Experimental("SKEXP0020")] private readonly IInMemoryVectorDb _iInMemoryVectorDb = new InMemoryIInMemoryVectorDb(new VectorDbConfig()
    {
        Name = "WikipediaText",
        VectorSize = applicationConfiguration.GetOllamaConfig.WikiVectorSize,
        DistantFunc = DistanceFunction.CosineSimilarity,
        IndexKind = IndexKind.Dynamic
    }, logger);

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<WikipediaDatasetModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> FindAsync(FilterDefinition<WikipediaDatasetModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<WikipediaDatasetModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> Where(Expression<Func<WikipediaDatasetModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<WikipediaDatasetModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public WikipediaDatasetModel? Get(string key)
    {
        return dataLayer.Get(key);
    }

    public Task<Result<WikipediaDatasetModel?>> Get(string key, params Expression<Func<WikipediaDatasetModel, object>>[] fieldsToFetch)
    {
        return dataLayer.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<WikipediaDatasetModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(WikipediaDatasetModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<WikipediaDatasetModel> GetAllAsync(Expression<Func<WikipediaDatasetModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return dataLayer.GetAllAsync(field2Fetch, cancellationToken);
    }

    [Experimental("SKEXP0020")]
    public async Task<Result<bool>> CreateAsync(WikipediaDatasetModel model, CancellationToken cancellationToken = default)
    {
        var result = await dataLayer.CreateAsync(model, cancellationToken);
        if (result.IsSuccess)
        {
            await parallelBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async serverToken => { await RequestIndex(model, serverToken); }, cancellationToken);
        }

        return result;
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<WikipediaDatasetModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(WikipediaDatasetModel model, CancellationToken cancellationToken = default)
    {
        FieldUpdate<WikipediaDatasetModel> update = new FieldUpdate<WikipediaDatasetModel>();
        update.UpdateAllFields(model);
        return dataLayer.UpdateAsync(model.Id.ToString(), update, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<WikipediaDatasetModel> updates, CancellationToken cancellationToken = default)
    {
        return dataLayer.UpdateAsync(key, updates, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<WikipediaDatasetModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    [Experimental("SKEXP0020")]
    public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _iInMemoryVectorDb.Init();
        Expression<Func<WikipediaDatasetModel, object>>[] expression =
        [
            model => model.Id,
            model => model.Title,
        ];
        var cursor = dataLayer.GetAllAsync(expression, cancellationToken);
        await foreach (var item in cursor)
        {
            await parallelBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async serverToken => await RequestIndex(item, serverToken), cancellationToken);
        }

        return Result<bool>.Success(true);
    }

    [Experimental("SKEXP0020")]
    private async Task RequestIndex(WikipediaDatasetModel item, CancellationToken cancellationToken = default)
    {
        var key = item.Id.ToString();
        if (vectorDataLayer.Exists("WikipediaText", key))
        {
            await foreach (var record in vectorDataLayer.GetAsyncEnumerator("WikipediaText", key, cancellationToken))
            {
                await _iInMemoryVectorDb.AddNewRecordAsync(new VectorRecord()
                {
                    Key = key,
                    Vector = record.Vector,
                    Title = item.Title,
                }, cancellationToken);
            }
            return;
        }
        
        foreach (var chunk in item.Text.ChunkText(12_000, 1_200))
        {
            var vector = await _iInMemoryVectorDb.GenerateVectorsFromDescription(chunk, cancellationToken);
            await vectorDataLayer.CreateAsync(new Models.Vector.VectorRecord()
            {
                Collection = "WikipediaText",
                Key = key,
                Vector = vector,
            }, cancellationToken);
            await _iInMemoryVectorDb.AddNewRecordAsync(new VectorRecord()
            {
                Key = key,
                Vector = vector,
                Title = item.Title,
            }, cancellationToken);
        }
    }

    public Task<Result<List<SearchScore<VectorRecord>>?>> SearchVectorAsync(float[] vector, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    [Experimental("SKEXP0020")]
    public void Dispose()
    {
        logger.LogInformation("Disposing WikipediaDataset memory dataset");
        _iInMemoryVectorDb.Dispose();
        logger.LogInformation("Disposed WikipediaDataset memory dataset");
    }

    [Experimental("SKEXP0020")]
    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("Disposing WikipediaDataset memory dataset");
        await _iInMemoryVectorDb.DisposeAsync();
        logger.LogInformation("Disposed WikipediaDataset memory dataset");
    }

    [Experimental("SKEXP0020")]
    public Task<List<SearchScore<VectorRecord>>> SearchRag(string query, int count, CancellationToken cancellationToken = default)
    {
        return _iInMemoryVectorDb.RagSearch(query, count, cancellationToken);
    }
}