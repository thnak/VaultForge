using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Business.Business.Interfaces.FileSystem;
using Business.Data.StorageSpace;
using Business.Models;
using Business.Models.RetrievalAugmentedGeneration.Semantic;
using Business.Services.RetrievalAugmentedGeneration.Interface;
using Business.Services.RetrievalAugmentedGeneration.Utils;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.EnumModel;
using BusinessModels.General.Results;
using BusinessModels.General.SettingModels;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace Business.Services.RetrievalAugmentedGeneration.Implement;

[Experimental("SKEXP0020")]
public class MovieDatabase : IMovieDatabase
{
    private IVectorStoreRecordCollection<int, Movie> Movies { get; }
    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; }
    private ILogger<MovieDatabase> Logger { get; }
    private IFileSystemBusinessLayer FileSystem { get; }
    private ISequenceBackgroundTaskQueue Queue { get; }
    private RedundantArrayOfIndependentDisks Raid { get; }
    private string OllamaApiEndpoint { get; }
    private string Image2TextModel { get; }

    public MovieDatabase(IFileSystemBusinessLayer fileSystemBusinessLayer, RedundantArrayOfIndependentDisks raid, ILogger<MovieDatabase> logger, ISequenceBackgroundTaskQueue queue, IOptions<AppSettings> options)
    {
        FileSystem = fileSystemBusinessLayer;
        Logger = logger;
        Queue = queue;
        Raid = raid;

        OllamaApiEndpoint = options.Value.OllamaConfig.ConnectionString;
        Image2TextModel = options.Value.OllamaConfig.Image2TextModel;

        var vectorStore = new InMemoryVectorStore();
        Movies = vectorStore.GetCollection<int, Movie>("movies");
        Generator = new OllamaEmbeddingGenerator(new Uri(OllamaApiEndpoint), options.Value.OllamaConfig.TextEmbeddingModel);
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

    public async Task RequestIndexAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<SearchResult<Movie>> SearchAsync(string query, int maxSize, CancellationToken cancellationToken = default)
    {
        var searchOptions = new VectorSearchOptions()
        {
            Top = maxSize,
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
        var field2Fetch = new Expression<Func<FileInfoModel, object>>[]
        {
            model => model.Id,
            model => model.Description,
            model => model.FileName,
            model => model.Vector,
        };
        var cursor = FileSystem.Where(model => model.Classify == FileClassify.Normal && model.ContentType.Contains("image"), cancellationToken, field2Fetch);
        int index = 0;
        await foreach (var file in cursor)
        {
            var key = index++;
            var fileId = file.Id.ToString();
            
            if (file.Vector.Any())
            {
                await Movies.UpsertAsync(new Movie()
                {
                    Description = file.Description,
                    Key = key,
                    Title = file.FileName,
                    Vector = file.Vector,
                }, cancellationToken: cancellationToken);
                continue;
            }

            await Queue.QueueBackgroundWorkItemAsync(async serverToken =>
            {
                using MemoryStream stream = new();
                await Raid.ReadGetDataAsync(stream, file.AbsolutePath, cancellationToken);
                stream.Seek(0, SeekOrigin.Begin);

                var description = await RetrievalAugmentedGenerationExtension.GenerateDescription(stream, $"{OllamaApiEndpoint}api/generate", Image2TextModel, serverToken);
                if (string.IsNullOrEmpty(description))
                {
                    Logger.LogError($"Description is null for {file.AbsolutePath}");
                    return;
                }

                var model = new Movie
                {
                    Key = key,
                    Title = file.FileName,
                    Description = description,
                    Vector = await Generator.GenerateEmbeddingVectorAsync(description, cancellationToken: serverToken)
                };
                
                await FileSystem.UpdateAsync(fileId, new FieldUpdate<FileInfoModel>()
                {
                    { x=> x.Description, description },
                    { x=> x.Vector, model.Vector }
                }, serverToken);
                
                await Movies.UpsertAsync(model, cancellationToken: serverToken);
            }, cancellationToken);
        }

        Logger.LogInformation($"Initialized total {index:N0} documents.");
    }

    #endregion
}