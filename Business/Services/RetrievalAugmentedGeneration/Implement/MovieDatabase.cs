using System.Diagnostics.CodeAnalysis;
using Business.Business.Interfaces.FileSystem;
using Business.Data.StorageSpace;
using Business.Models;
using Business.Models.RetrievalAugmentedGeneration.Semantic;
using Business.Services.RetrievalAugmentedGeneration.Interface;
using Business.Services.RetrievalAugmentedGeneration.Utils;
using Business.Services.TaskQueueServices.Base.Interfaces;
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
    private ISequenceBackgroundTaskQueue Queue { get; set; }
    private RedundantArrayOfIndependentDisks Raid { get; set; }

    public MovieDatabase(IFileSystemBusinessLayer fileSystemBusinessLayer, RedundantArrayOfIndependentDisks raid, ILogger<MovieDatabase> logger, ISequenceBackgroundTaskQueue queue)
    {
        FileSystem = fileSystemBusinessLayer;
        Logger = logger;
        Queue = queue;
        Raid = raid;

        var vectorStore = new InMemoryVectorStore();
        Movies = vectorStore.GetCollection<int, Movie>("movies");
        Generator = new OllamaEmbeddingGenerator(new Uri("http://localhost:11434/"), "all-minilm");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Movies.CreateCollectionIfNotExistsAsync(cancellationToken);
        await InitDescription(cancellationToken);
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

    private async Task InitDescription(CancellationToken cancellationToken = default)
    {
        var cursor = FileSystem.Where(model => model.Classify == FileClassify.Normal && model.ContentType.Contains("image"), cancellationToken, model => model.Id, model => model.FileName, model => model.AbsolutePath);
        int index = 0;
        await foreach (var file in cursor)
        {
            index++;
            await Queue.QueueBackgroundWorkItemAsync(async serverToken =>
            {
                using MemoryStream stream = new();
                await Raid.ReadGetDataAsync(stream, file.AbsolutePath, cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);

                var description = await RetrievalAugmentedGenerationExtension.GenerateDescription(stream, serverToken);
                if (string.IsNullOrEmpty(description))
                {
                    Logger.LogError($"Description is null for {file.AbsolutePath}");
                    return;
                }

                var model = new Movie
                {
                    Key = file.Id,
                    Title = file.FileName,
                    Vector = await Generator.GenerateEmbeddingVectorAsync(description, cancellationToken: serverToken)
                };
                await Movies.UpsertAsync(model, cancellationToken: serverToken);
            }, cancellationToken);
        }

        Logger.LogInformation($"Initialized total {index:N0} documents.");
    }

    #endregion
}