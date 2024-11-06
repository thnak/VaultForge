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
public class VectorDb : IVectorDb
{
    private IVectorStoreRecordCollection<int, VectorRecord> Collection { get; set; }
    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; set; }
    private SemaphoreSlim Semaphore { get; } = new(1, 1);
    private ILogger Logger { get; set; }
    private int TotalRecord { get; set; }
    private bool _disposed;
    private readonly string ConnectionString;
    private readonly string Image2TextModelName;

    public VectorDb(VectorDbConfig config, ILogger logger)
    {
        Logger = logger;
        var vectorStore = new InMemoryVectorStore();
        ConnectionString = config.OllamaConnectionString;
        Image2TextModelName = config.OllamaImage2TextModelName;

        Collection = vectorStore.GetCollection<int, VectorRecord>(config.Name);
        Generator = new OllamaEmbeddingGenerator(new Uri(ConnectionString), config.OllamaTextEmbeddingModelName);
    }

    private async Task InitSampleData()
    {
        var movieData = new List<VectorRecord>()
        {
            new VectorRecord
            {
                Index = 0,
                Title = "Lion King",
                Description = "The Lion King is a classic Disney animated film that tells the story of a young lion named Simba who embarks on a journey to reclaim his throne as the king of the Pride Lands after the tragic death of his father."
            },
            new VectorRecord
            {
                Index = 1,
                Title = "Inception",
                Description = "Inception is a science fiction film directed by Christopher Nolan that follows a group of thieves who enter the dreams of their targets to steal information."
            },
            new VectorRecord
            {
                Index = 2,
                Title = "The Matrix",
                Description = "The Matrix is a science fiction film directed by the Wachowskis that follows a computer hacker named Neo who discovers that the world he lives in is a simulated reality created by machines."
            },
            new VectorRecord
            {
                Index = 3,
                Title = "Shrek",
                Description = "Shrek is an animated film that tells the story of an ogre named Shrek who embarks on a quest to rescue Princess Fiona from a dragon and bring her back to the kingdom of Duloc."
            }
        };
        foreach (var movie in movieData)
        {
            movie.Vector = await Generator.GenerateEmbeddingVectorAsync(movie.Description);
            await Collection.UpsertAsync(movie);
        }
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
        var searchOptions = new VectorSearchOptions()
        {
            Top = count,
            VectorPropertyName = "Vector",
            IncludeVectors = false,
            IncludeTotalCount = true
        };
        var queryEmbedding = await Generator.GenerateEmbeddingVectorAsync(query, cancellationToken: cancellationToken);
        var cursor = await Collection.VectorizedSearchAsync(queryEmbedding, searchOptions, cancellationToken: cancellationToken);
        await foreach (var result in cursor.Results.WithCancellation(cancellationToken))
        {
            yield return new SearchScore<VectorRecord>(result.Record, result.Score ?? 0);
        }
    }

    public async IAsyncEnumerable<SearchScore<VectorRecord>> Search(float[] vector, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

    public async Task Init()
    {
        Logger.LogInformation($"[VectorDB][{Collection.CollectionName}] Initializing...");
        await Collection.CreateCollectionIfNotExistsAsync();
        await InitSampleData();
    }

    public void Dispose()
    {
        if (_disposed) return;

        Generator.Dispose();
        Semaphore.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this); // Prevents finalizer if Dispose was called.
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await Collection.DeleteCollectionAsync();

        _disposed = true;
        GC.SuppressFinalize(this); // Prevents finalizer if DisposeAsync was called.
    }
}