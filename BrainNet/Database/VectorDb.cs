using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BrainNet.Models;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace BrainNet.Database;

[Experimental("SKEXP0020")]
public class VectorDb : IVectorDb
{
    private IVectorStoreRecordCollection<int, VectorRecord> Movies { get; set; }
    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; set; }
    private SemaphoreSlim Semaphore { get; } = new(1, 1);
    private int TotalRecord { get; set; }

    public VectorDb(VectorDbConfig config)
    {
        var vectorStore = new InMemoryVectorStore();
        Movies = vectorStore.GetCollection<int, VectorRecord>(config.Name);
        Generator = new OllamaEmbeddingGenerator(new Uri(config.OllamaConnectionString), config.OllamaTextEmbeddingModelName);
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
            await Movies.UpsertAsync(movie);
        }
    }

    public async Task AddNewRecordAsync(VectorRecord vectorRecord, CancellationToken cancellationToken = default)
    {
        try
        {
            await Semaphore.WaitAsync(cancellationToken);
            vectorRecord.Index = TotalRecord++;
            if (vectorRecord.Vector.Length < 0)
            {
                vectorRecord.Vector = await Generator.GenerateEmbeddingVectorAsync(vectorRecord.Description, cancellationToken: cancellationToken);
            }

            await Movies.UpsertAsync(vectorRecord, null, cancellationToken);
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

    public async IAsyncEnumerable<VectorRecord> Search(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await Generator.GenerateEmbeddingVectorAsync(query, cancellationToken: cancellationToken);
        var cursor = await Movies.VectorizedSearchAsync(queryEmbedding, cancellationToken: cancellationToken);
        await foreach (var result in cursor.Results.WithCancellation(cancellationToken))
        {
            yield return result.Record;
        }
    }

    public async Task Init()
    {
        await Movies.CreateCollectionIfNotExistsAsync();
        await InitSampleData();
    }

    public void Dispose()
    {
        Generator.Dispose();
        Semaphore.Dispose();
    }
}