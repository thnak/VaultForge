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
public class FileInfoVectorDb : IFileInfoVectorDb
{
    private IVectorStoreRecordCollection<int, FileVectorModel> FileInfoCollection { get; }
    private IEmbeddingGenerator<string, Embedding<float>> Generator { get; }
    private ILogger<FileInfoVectorDb> Logger { get; }
    private IFileSystemBusinessLayer FileSystem { get; }
    private ISequenceBackgroundTaskQueue Queue { get; }
    private RedundantArrayOfIndependentDisks Raid { get; }
    private string OllamaApiEndpoint { get; }
    private string Image2TextModel { get; }
    private int TotalIndexed { get; set; }

    public FileInfoVectorDb(IFileSystemBusinessLayer fileSystemBusinessLayer, RedundantArrayOfIndependentDisks raid, ILogger<FileInfoVectorDb> logger, ISequenceBackgroundTaskQueue queue, IOptions<AppSettings> options)
    {
        FileSystem = fileSystemBusinessLayer;
        Logger = logger;
        Queue = queue;
        Raid = raid;

        OllamaApiEndpoint = options.Value.OllamaConfig.ConnectionString;
        Image2TextModel = options.Value.OllamaConfig.Image2TextModel;

        var vectorStore = new InMemoryVectorStore();
        FileInfoCollection = vectorStore.GetCollection<int, FileVectorModel>(nameof(FileInfoModel));
        Generator = new OllamaEmbeddingGenerator(new Uri(OllamaApiEndpoint), options.Value.OllamaConfig.TextEmbeddingModel);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await FileInfoCollection.CreateCollectionIfNotExistsAsync(cancellationToken);
        await InitDescription(cancellationToken);
    }

    public void Dispose()
    {
        FileInfoCollection.DeleteCollectionAsync();
    }

    public async Task AddAsync(FileVectorModel entity, CancellationToken cancellationToken = default)
    {
        var file = FileSystem.Get(entity.FileId);
        if(file is null) return;
        entity.Vector = await Generator.GenerateEmbeddingVectorAsync(file.Description, cancellationToken: cancellationToken);
        await FileInfoCollection.UpsertAsync(entity, null, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<FileVectorModel> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await AddAsync(entity, cancellationToken);
        }
    }

    public async Task RequestIndexAsync(string key, CancellationToken cancellationToken = default)
    {
        var file = FileSystem.Get(key);
        if (file != null)
        {
            if (!file.Vector.Any())
            {
                await Queue.QueueBackgroundWorkItemAsync(async serverToken => { await Request(file, serverToken); }, cancellationToken);
            }
        }
    }

    private async Task Request(FileInfoModel file, CancellationToken cancellationToken = default)
    {
        using MemoryStream stream = new();
        await Raid.ReadGetDataAsync(stream, file.AbsolutePath, cancellationToken);
        stream.Seek(0, SeekOrigin.Begin);

        var description = await RetrievalAugmentedGenerationExtension.GenerateDescription(stream, $"{OllamaApiEndpoint}api/generate", Image2TextModel, cancellationToken);
        if (string.IsNullOrEmpty(description))
        {
            Logger.LogError($"Description is null for {file.AbsolutePath}");
            return;
        }

        var vector = (await Generator.GenerateEmbeddingVectorAsync(description, cancellationToken: cancellationToken)).ToArray();
        var model = new FileVectorModel
        {
            Index = TotalIndexed++,
            FileId = file.Id.ToString(),
            Vector = vector,
        };

        await FileSystem.UpdateAsync(file.Id.ToString(), new FieldUpdate<FileInfoModel>()
        {
            { x => x.Description, description },
            { x => x.Vector, vector }
        }, cancellationToken);

        await FileInfoCollection.UpsertAsync(model, cancellationToken: cancellationToken);
    }

    public async Task<SearchResult<FileVectorModel>> SearchAsync(string query, int maxSize, CancellationToken cancellationToken = default)
    {
        var searchOptions = new VectorSearchOptions()
        {
            Top = maxSize,
            VectorPropertyName = "Vector",
            IncludeVectors = false,
            IncludeTotalCount = true
        };

        var queryEmbedding = await Generator.GenerateEmbeddingVectorAsync(query, cancellationToken: cancellationToken);
        var cursor = await FileInfoCollection.VectorizedSearchAsync(queryEmbedding, searchOptions, cancellationToken: cancellationToken);
        List<SearchScore<FileVectorModel>> result = new();
        await foreach (var item in cursor.Results.WithCancellation(cancellationToken))
        {
            result.Add(new SearchScore<FileVectorModel>(item.Record, item.Score ?? 0));
        }

        return SearchResult<FileVectorModel>.Success(result);
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
            model => model.AbsolutePath
        };
        var cursor = FileSystem.Where(model => model.Classify == FileClassify.Normal && model.ContentType.Contains("image"), cancellationToken, field2Fetch);
        int index = 0;
        await foreach (var file in cursor)
        {
            var key = index++;
            if (file.Vector.Any())
            {
                await FileInfoCollection.UpsertAsync(new FileVectorModel()
                {
                    Index = key,
                    FileId = file.Id.ToString(),
                    Vector = file.Vector,
                }, cancellationToken: cancellationToken);
                continue;
            }

            await Queue.QueueBackgroundWorkItemAsync(async serverToken => { await Request(file, serverToken); }, cancellationToken);
        }

        Logger.LogInformation($"Initialized total {index:N0} documents.");
    }

    #endregion
}