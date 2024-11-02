using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BrainNet.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace BrainNet.Service;

[Experimental("SKEXP0020")]
public class VectorDb
{
    private InMemoryVectorStore VectorStore { get; set; }
    private IVectorStoreRecordCollection<int, Movie> Movies { get; set; }
    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; set; }

    public VectorDb()
    {
        VectorStore = new InMemoryVectorStore();
        Movies = VectorStore.GetCollection<int, Movie>("movies");
        Generator = new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");
    }

    private async Task InitSampleData()
    {
        var movieData = new List<Movie>()
        {
            new Movie
            {
                Key = 0,
                Title = "Lion King",
                Description = "The Lion King is a classic Disney animated film that tells the story of a young lion named Simba who embarks on a journey to reclaim his throne as the king of the Pride Lands after the tragic death of his father."
            },
            new Movie
            {
                Key = 1,
                Title = "Inception",
                Description = "Inception is a science fiction film directed by Christopher Nolan that follows a group of thieves who enter the dreams of their targets to steal information."
            },
            new Movie
            {
                Key = 2,
                Title = "The Matrix",
                Description = "The Matrix is a science fiction film directed by the Wachowskis that follows a computer hacker named Neo who discovers that the world he lives in is a simulated reality created by machines."
            },
            new Movie
            {
                Key = 3,
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

    public async Task AddMovie(Movie movie, CancellationToken cancellationToken = default)
    {
        
        await Movies.UpsertAsync(movie, null, cancellationToken);
    }

    public async IAsyncEnumerable<Movie> Search(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
}