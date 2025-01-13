using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Data.StorageSpace;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Business.Utils;
using Business.Utils.ExpressionExtensions;
using Business.Utils.Protector;
using Business.Utils.StringExtensions;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.FileSystem;

public class FileSystemDatalayer(
    IMongoDataLayerContext context,
    IDataProtectionProvider provider,
    ILogger<FileSystemDatalayer> logger,
    ISequenceBackgroundTaskQueue sequenceQueue,
    IMemoryCache memoryCache,
    RedundantArrayOfIndependentDisks raidService) : IFileSystemDatalayer
{
    private readonly IMongoCollection<FileInfoModel> _fileDataDb = context.MongoDatabase.GetCollection<FileInfoModel>("FileInfo");
    private readonly IMongoCollection<FileMetadataModel> _fileMetaDataDataDb = context.MongoDatabase.GetCollection<FileMetadataModel>("FileMetaData");
    private readonly IDataProtector _protectionProvider = provider.CreateProtector("FileSystemDatalayerProtector");

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IndexKeysDefinition<FileInfoModel>[] uniqueIndexesDefinitions =
            [
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.AliasCode)
            ];
            IndexKeysDefinition<FileInfoModel>[] indexKeysDefinitions =
            [
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.Id),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Descending(x => x.Id),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.CreatedDate),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Descending(x => x.CreatedDate),

                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.Status),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.Classify),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.Classify).Ascending(x => x.Status),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.ContentType),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.CreatedDate),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.ModifiedTime),
                Builders<FileInfoModel>.IndexKeys.Descending(x => x.ModifiedTime),

                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.ModifiedTime),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Descending(x => x.ModifiedTime),

                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.RelativePath),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Descending(x => x.RelativePath),

                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.FileName),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Descending(x => x.FileName),

                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.ContentType).Ascending(x => x.RelativePath),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.ContentType).Descending(x => x.RelativePath),


                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.CreatedDate).Ascending(x => x.Status),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.CreatedDate).Ascending(x => x.Classify),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.CreatedDate).Ascending(x => x.ContentType),

                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.Status),
                Builders<FileInfoModel>.IndexKeys.Ascending(x => x.ContentType),
            ];
            var modelIndexes = indexKeysDefinitions.Select(x => new CreateIndexModel<FileInfoModel>(x));
            var uniqueIndexes = uniqueIndexesDefinitions.Select(x => new CreateIndexModel<FileInfoModel>(x, new CreateIndexOptions { Unique = true }));

            await _fileDataDb.Indexes.DropAllAsync(cancellationToken);
            await _fileDataDb.Indexes.CreateManyAsync([..modelIndexes, ..uniqueIndexes], cancellationToken);

            logger.LogInformation(@"[Init] File info data layer");

            var metaKey = Builders<FileMetadataModel>.IndexKeys.Ascending(x => x.ThumbnailAbsolutePath);
            var metaIndexModel = new CreateIndexModel<FileMetadataModel>(metaKey, new CreateIndexOptions { Unique = true });


            await _fileMetaDataDataDb.Indexes.CreateOneAsync(metaIndexModel, cancellationToken: cancellationToken);

            return (true, string.Empty);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex.Message);
            return (false, string.Empty);
        }
        catch (MongoException ex)
        {
            logger.LogError(ex, null);
            return (false, ex.Message);
        }
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return _fileDataDb.GetDocumentSizeAsync(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(0L);
        }
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return _fileDataDb.GetDocumentSizeAsync(predicate, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(0L);
        }
    }


    public IAsyncEnumerable<FileInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return _fileDataDb.FindProjectAsync(f => f.FileName.Contains(queryString), null, cancellationToken);
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return _fileDataDb.FindProjectAsync(f => f.FileName.Contains(keyWord), null, cancellationToken);
    }

    public IAsyncEnumerable<FileInfoModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return _fileDataDb.FindProjectAsync(f => f.FileName.Contains(keyWord), limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<FileInfoModel> WhereAsync(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return _fileDataDb.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public FileInfoModel? Get(string key)
    {
        FilterDefinition<FileInfoModel> filter;
        if (ObjectId.TryParse(key, out var id))
        {
            filter = Builders<FileInfoModel>.Filter.Eq(x => x.Id, id);
        }
        else
        {
            filter = Builders<FileInfoModel>.Filter.Eq(x => x.AbsolutePath, key);
            filter |= Builders<FileInfoModel>.Filter.Eq(x => x.AliasCode, key);
        }

        return _fileDataDb.Find(filter).Limit(1).FirstOrDefault();
    }

    public Task<Result<FileInfoModel?>> Get(string key, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        return _fileDataDb.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> GetAllAsync(Expression<Func<FileInfoModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _fileDataDb.GetAll(field2Fetch, cancellationToken);
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FileInfoModel> updates, CancellationToken cancellationToken = default)
    {
        var oldValue = Get(key);
        if(oldValue == null)
            return (false, AppLang.NotFound);
        var updateResult = await _fileDataDb.UpdateAsync(oldValue.Id.ToString(), updates, cancellationToken);
        return (updateResult.IsSuccess, updateResult.Message);
    }

    public async Task<Result<bool>> CreateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.Id, model.Id);
            var isExist = await _fileDataDb.Find(filter).AnyAsync(cancellationToken: cancellationToken);
            if (!isExist)
            {
                model.CreateTime = DateTime.UtcNow;
                model.ModifiedTime = DateTime.UtcNow;
                if (string.IsNullOrEmpty(model.AliasCode))
                {
                    model.AliasCode = model.Id.GenerateAliasKey(DateTime.Now.Ticks.ToString());
                }

                if (string.IsNullOrEmpty(model.ContentType))
                    model.ContentType = "application/octet-stream";

                await _fileDataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(AppLang.File_is_already_exsists, ErrorType.NotFound);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<FileInfoModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> ReplaceAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var isExists = await _fileDataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
            if (!isExists)
            {
                return (false, AppLang.File_could_not_be_found);
            }

            model.ModifiedTime = DateTime.UtcNow;
            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.Id, model.Id);
            await _fileDataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationToken);
            return (true, AppLang.Create_successfully);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return (false, "Operation cancelled");
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
            return (false, e.Message);
        }
    }


    public async IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<FileInfoModel> models, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in models.TakeWhile(_ => cancellationToken is not { IsCancellationRequested: true }))
        {
            var result = await ReplaceAsync(file, cancellationToken);
            yield return (true, result.Item2, file.Id.ToString());
        }

        yield return (true, AppLang.Success, string.Empty);
    }

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return (false, AppLang.File_could_not_be_found);

            var query = Get(key);
            if (query == null) return (false, AppLang.File_not_found_);

            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.AbsolutePath, key);
            if (ObjectId.TryParse(key, out var id)) filter |= Builders<FileInfoModel>.Filter.Eq(x => x.Id, id);

            await _fileDataDb.DeleteManyAsync(filter, cancelToken);
            await raidService.DeleteAsync(query.AbsolutePath);

            DeleteMetadata(query.MetadataId);
            return (true, AppLang.Delete_successfully);
        }
        catch (OperationCanceledException)
        {
            return (false, AppLang.Cancel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, null);
            return (false, ex.Message);
        }
        finally
        {
            await sequenceQueue.QueueBackgroundWorkItemAsync(async (serverToken) =>
            {
                List<string> extendFiles = new List<string>();
                await foreach (var extendFile in WhereAsync(file => file.ParentResource == key, serverToken, model => model.Id))
                {
                    extendFiles.Add(extendFile.Id.ToString());
                }

                foreach (var extendFile in extendFiles)
                {
                    await DeleteAsync(extendFile, serverToken);
                }
            });
        }
    }

    public FileMetadataModel? GetMetaData(string metaId)
    {
        if (ObjectId.TryParse(metaId, out var id))
        {
            var filter = Builders<FileMetadataModel>.Filter.Eq(x => x.Id, id);
            return _fileMetaDataDataDb.Find(filter).Limit(1).FirstOrDefault();
        }

        return default;
    }

    public (bool, string) DeleteMetadata(string metaId)
    {
        if (ObjectId.TryParse(metaId, out var id))
        {
            var metadata = GetMetaData(metaId);
            if (metadata == null) return (false, AppLang.Could_not_found_metadata);
            var filter = Builders<FileMetadataModel>.Filter.Eq(x => x.Id, id);
            _fileMetaDataDataDb.DeleteOne(filter);
            try
            {
                File.Delete(metadata.ThumbnailAbsolutePath);
            }
            catch (Exception e)
            {
                logger.LogError(e, null);
            }

            return (true, AppLang.Delete_successfully);
        }

        return (false, AppLang.Incorrect_metadata_ID);
    }

    public long GetFileSize(Expression<Func<FileInfoModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<FileInfoModel?> GetRandomFileAsync(string rootFolderId, CancellationToken cancellationToken = default)
    {
        // Use aggregation to randomly sample one file
        var pipeline = new[]
        {
            new BsonDocument { { "$match", new BsonDocument(nameof(FileInfoModel.RootFolder), rootFolderId) } },
            new BsonDocument { { "$sample", new BsonDocument("size", 1) } } // Randomly pick one document
        };

        var result = await _fileDataDb.AggregateAsync<FileInfoModel>(pipeline, cancellationToken: cancellationToken);

        return await result.FirstOrDefaultAsync(cancellationToken);
    }

    public async IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        if (ObjectId.TryParse(id, out ObjectId objectId))
        {
            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.Id, objectId);
            var options = new FindOptions<FileInfoModel, FileInfoModel>
            {
                Projection = fieldsToFetch.ProjectionBuilder(),
                Limit = pageSize,
                Skip = pageSize * pageNumber,
            };
            using var cursor = await _fileDataDb.FindAsync(filter, options, cancellationToken: cancellationToken);
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var model in cursor.Current)
                {
                    yield return model;
                }
            }
        }
        else
        {
            logger.LogError($"[ERROR] ID is incorrect");
        }
    }

    public async IAsyncEnumerable<FileInfoModel> GetContentFormParentFolderAsync(Expression<Func<FileInfoModel, bool>> predicate, int pageNumber, int pageSize, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        ObjectId? lastSeenId = null;

        bool hasIdCheck = predicate.PredicateContainsIdCheck(f => f.Id);
        var stringKey = predicate.GetCacheKey();

        if (!hasIdCheck && memoryCache.TryGetValue<ObjectId>(stringKey, out var cachedLastSeenId))
        {
            lastSeenId = cachedLastSeenId;
        }

        var options = new FindOptions<FileInfoModel, FileInfoModel>
        {
            Limit = pageSize,
            Skip = lastSeenId == null ? pageSize * pageNumber : 0,
        };
        if (fieldsToFetch.Any())
            options.Projection = fieldsToFetch.ProjectionBuilder();

        var filterBuilder = Builders<FileInfoModel>.Filter;
        var filter = Builders<FileInfoModel>.Filter.Empty;

        filter = lastSeenId.HasValue ? filterBuilder.And(filter, filterBuilder.Lt(f => f.Id, lastSeenId.Value)) : Builders<FileInfoModel>.Filter.Where(predicate);

        using var cursor = await _fileDataDb.FindAsync(filter, options, cancellationToken: cancellationToken);

        ObjectId? currentLastSeenId = null;


        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
                currentLastSeenId = model.Id;
            }
        }

        if (currentLastSeenId.HasValue && hasIdCheck)
        {
            memoryCache.Set(stringKey, currentLastSeenId.Value, TimeSpan.FromSeconds(10));
        }
    }

    public void Dispose()
    {
        //
    }
}