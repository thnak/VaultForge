﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BrainNet.Models.Result;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using BrainNet.Utils;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace BrainNet.Database;

[Experimental("SKEXP0020")]
public class VectorDb : IVectorDb
{
    private IVectorStoreRecordCollection<int, VectorRecord> Collection { get; }
    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; }
    private SemaphoreSlim Semaphore { get; } = new(1, 1);
    private ILogger Logger { get; set; }
    private bool _disposed;
    private string ConnectionString { get; }
    private string Image2TextModelName { get; }
    private double SearchThresholds { get; }

    public VectorDb(VectorDbConfig config, ILogger logger)
    {
        Logger = logger;
        var vectorStore = new InMemoryVectorStore();
        ConnectionString = config.OllamaConnectionString;
        Image2TextModelName = config.OllamaImage2TextModelName;
        SearchThresholds = config.SearchThresholds;

        Collection = vectorStore.GetCollection<int, VectorRecord>(config.Name);
        Generator = new OllamaEmbeddingGenerator(new Uri(ConnectionString), config.OllamaTextEmbeddingModelName);
    }
    
    public async Task AddNewRecordAsync(VectorRecord vectorRecord, CancellationToken cancellationToken = default)
    {
        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            // vectorRecord.Index = TotalRecord++;
            if (vectorRecord.Vector.Length < 0)
            {
                vectorRecord.Vector = await Generator.GenerateEmbeddingVectorAsync(vectorRecord.Description, cancellationToken: cancellationToken);
            }

            await Collection.UpsertAsync(vectorRecord, null, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            //
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task AddNewRecordAsync(IReadOnlyCollection<VectorRecord> vectorRecords, CancellationToken cancellationToken = default)
    {
        await foreach (var _ in Collection.UpsertBatchAsync(vectorRecords, cancellationToken: cancellationToken))
        {
            //
        }
    }

    public async Task DeleteRecordAsync(int key, CancellationToken cancellationToken = default)
    {
        await Collection.DeleteAsync(key, cancellationToken: cancellationToken);
    }

    public Task DeleteRecordAsync(IReadOnlyCollection<int> keys, CancellationToken cancellationToken = default)
    {
        return Collection.DeleteBatchAsync(keys, cancellationToken: cancellationToken);
    }

    public async Task<float[]> GenerateVectorsFromDescription(string description, CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await Generator.GenerateEmbeddingVectorAsync(description, cancellationToken: cancellationToken);
        return queryEmbedding.ToArray();
    }

    public async Task<string> GenerateImageDescription(MemoryStream stream, CancellationToken cancellationToken = default)
    {
        var description = await stream.GenerateDescription(ConnectionString + "api/generate", Image2TextModelName, cancellationToken);
        return description;
    }

    public async IAsyncEnumerable<SearchScore<VectorRecord>> Search(string query, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await Generator.GenerateEmbeddingVectorAsync(query, cancellationToken: cancellationToken);
        await foreach (var result in Search(queryEmbedding, count, cancellationToken))
        {
            yield return result;
        }
    }

    public async IAsyncEnumerable<SearchScore<VectorRecord>> Search(ReadOnlyMemory<float> vector, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var searchOptions = new VectorSearchOptions()
        {
            Top = count,
            VectorPropertyName = "Vector",
            IncludeVectors = false,
            IncludeTotalCount = true
        };
        var cursor = await Collection.VectorizedSearchAsync(vector, searchOptions, cancellationToken: cancellationToken);
        await foreach (var result in cursor.Results.WithCancellation(cancellationToken))
        {
            var score = result.Score ?? 0;
            if(score < SearchThresholds) continue;
            
            yield return new SearchScore<VectorRecord>(result.Record, result.Score ?? 0);
        }
    }

    public async Task Init()
    {
        Logger.LogInformation($"[VectorDB][{Collection.CollectionName}] Initializing...");
        await Collection.CreateCollectionIfNotExistsAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;

        Generator.Dispose();
        Semaphore.Dispose();

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await Collection.DeleteCollectionAsync();
        Generator.Dispose();
        Semaphore.Dispose();
        _disposed = true;
    }
}