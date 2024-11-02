using System.Diagnostics.CodeAnalysis;
using Business.Business.Interfaces.FileSystem;
using Business.Models.RetrievalAugmentedGeneration.Semantic;
using Business.Services.RetrievalAugmentedGeneration.Interface;
using BusinessModels.General.EnumModel;
using BusinessModels.General.Results;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Business.Services.RetrievalAugmentedGeneration.Implement;

[Experimental("SKEXP0020")]
public class MovieDatabase : IMovieDatabase
{
    private IVectorStoreRecordCollection<int, Movie> Movies { get; set; }
    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; set; }
    private ILogger<MovieDatabase> Logger { get; set; }
    private IFileSystemBusinessLayer FileSystem { get; set; }
    
    public MovieDatabase(IFileSystemBusinessLayer fileSystemBusinessLayer, ILogger<MovieDatabase> logger)
    {
        FileSystem = fileSystemBusinessLayer;
        Logger = logger;
        
        var vectorStore = new InMemoryVectorStore();
        Movies = vectorStore.GetCollection<int, Movie>("movies");
        Generator = new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Movies.CreateCollectionIfNotExistsAsync(cancellationToken);
        await InitSampleData();
    }

    public void Dispose()
    {
        Movies.DeleteCollectionAsync();
    }

    public async Task AddAsync(Movie entity, CancellationToken cancellationToken = default)
    {
        entity.Vector = await Generator.GenerateEmbeddingVectorAsync(entity.Description, cancellationToken: cancellationToken);
        await Movies.UpsertAsync(entity, null, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Movie> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await AddAsync(entity, cancellationToken);
        }
    }

    public async Task<SearchResult<Movie>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchOptions = new VectorSearchOptions()
        {
            Top = 1,
            VectorPropertyName = "Vector",
            IncludeVectors = false,
            IncludeTotalCount = true
        };

        var queryEmbedding = await Generator.GenerateEmbeddingVectorAsync(query, cancellationToken: cancellationToken);
        var cursor = await Movies.VectorizedSearchAsync(queryEmbedding, searchOptions, cancellationToken: cancellationToken);
        List<SearchScore<Movie>> result = new();
        await foreach (var item in cursor.Results.WithCancellation(cancellationToken))
        {
            result.Add(new SearchScore<Movie>(item.Record, item.Score ?? 0));
        }

        return SearchResult<Movie>.Success(result);
    }

    #region Private functions

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

    private async Task InitDescription()
    {
        var files = new List<FileInfoModel>();
        var cursor = FileSystem.Where(model => model.Classify == FileClassify.Normal && model.ContentType == "image/jpeg");
        
    }

    #endregion
}