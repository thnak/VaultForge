using System.Diagnostics.CodeAnalysis;
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
public class InMemoryIInMemoryVectorDb : IInMemoryVectorDb
{
    private IVectorStoreRecordCollection<Guid, VectorRecord> Collection { get; }

    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; }

    private ILogger Logger { get; set; }
    private bool _disposed;
    private string ConnectionString { get; }
    private string Image2TextModelName { get; }

    public InMemoryIInMemoryVectorDb(VectorDbConfig config, ILogger logger)
    {
        Logger = logger;
        ConnectionString = config.OllamaConnectionString;
        Image2TextModelName = config.OllamaImage2TextModelName;

        var productDefinition = new VectorStoreRecordDefinition
        {
            Properties = new List<VectorStoreRecordProperty>
            {
                new VectorStoreRecordKeyProperty(nameof(VectorRecord.Index), typeof(Guid)),
                new VectorStoreRecordDataProperty(nameof(VectorRecord.Key), typeof(string)) { IsFilterable = true, IsFullTextSearchable = true },
                new VectorStoreRecordDataProperty(nameof(VectorRecord.Description), typeof(string)) { IsFullTextSearchable = true },
                new VectorStoreRecordDataProperty(nameof(VectorRecord.Title), typeof(string)),
                new VectorStoreRecordVectorProperty(nameof(VectorRecord.Vector), typeof(ReadOnlyMemory<float>))
                {
                    Dimensions = config.VectorSize,
                    DistanceFunction = config.DistantFunc,
                    IndexKind = config.IndexKind,
                }
            }
        };

        Collection = new InMemoryVectorStoreRecordCollection<Guid, VectorRecord>(config.Name, new InMemoryVectorStoreRecordCollectionOptions<Guid, VectorRecord>()
        {
            VectorStoreRecordDefinition = productDefinition
        });
        Generator = new OllamaEmbeddingGenerator(new Uri(ConnectionString), config.OllamaTextEmbeddingModelName);
    }

    public async Task AddNewRecordAsync(VectorRecord vectorRecord, CancellationToken cancellationToken = default)
    {
        try
        {
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
    }

    public async Task AddNewRecordAsync(IReadOnlyCollection<VectorRecord> vectorRecords, CancellationToken cancellationToken = default)
    {
        await foreach (var _ in Collection.UpsertBatchAsync(vectorRecords, cancellationToken: cancellationToken))
        {
            //
        }
    }

    public async Task DeleteRecordAsync(Guid key, CancellationToken cancellationToken = default)
    {
        await Collection.DeleteAsync(key, cancellationToken: cancellationToken);
    }

    public Task DeleteRecordAsync(IReadOnlyCollection<Guid> keys, CancellationToken cancellationToken = default)
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
            yield return new SearchScore<VectorRecord>(result.Record, result.Score ?? 0);
        }
    }

    public async Task<long> Count(IReadOnlyCollection<float> vector, CancellationToken cancellationToken = default)
    {
        var searchOptions = new VectorSearchOptions()
        {
            VectorPropertyName = "Vector",
            IncludeVectors = false,
            IncludeTotalCount = true
        };
        var cursor = await Collection.VectorizedSearchAsync(vector, searchOptions, cancellationToken: cancellationToken);
        return cursor.TotalCount ?? 0;
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
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await Collection.DeleteCollectionAsync();
        Generator.Dispose();
        _disposed = true;
    }
}