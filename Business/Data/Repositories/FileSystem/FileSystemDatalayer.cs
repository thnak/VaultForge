using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using Business.Utils;
using Business.Utils.ExpressionExtensions;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.FileSystem;

public class FileSystemDatalayer(IMongoDataLayerContext context, ILogger<FileSystemDatalayer> logger, IMemoryCache memoryCache) : IFileSystemDatalayer
{
    private readonly IMongoCollection<FileInfoModel> _fileDataDb = context.MongoDatabase.GetCollection<FileInfoModel>("FileInfo");
    private readonly IMongoCollection<FileMetadataModel> _fileMetaDataDataDb = context.MongoDatabase.GetCollection<FileMetadataModel>("FileMetaData");
    private readonly SemaphoreSlim _semaphore = new(100, 1000);
    private const string SearchIndexNameLastSeenId = "FileInfoSearchIndexLastSeenId";

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var absolutePathKey = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath);
            var absolutePathIndexModel = new CreateIndexModel<FileInfoModel>(absolutePathKey, new CreateIndexOptions { Unique = true });

            var rootFolderIndexKeysDefinition = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder);
            var rootFolderIndexModel = new CreateIndexModel<FileInfoModel>(rootFolderIndexKeysDefinition);

            var rootFolderAndCreateDateKeysDefinition = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.CreatedDate);
            var rootFolderAndCreateDateIndexModel = new CreateIndexModel<FileInfoModel>(rootFolderAndCreateDateKeysDefinition);
            
            var rootAndTypeKey = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.Type);
            var rootAndTypeIndexModel = new CreateIndexModel<FileInfoModel>(rootAndTypeKey, new CreateIndexOptions { Unique = false });

            var createDateKeysDefinition = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.CreatedDate);
            var createDateIndexModel = new CreateIndexModel<FileInfoModel>(createDateKeysDefinition);

            var createDateAndTypeKeysDefinition = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.CreatedDate).Ascending(x => x.Type);
            var createDateAndTypeIndexModel = new CreateIndexModel<FileInfoModel>(createDateAndTypeKeysDefinition);

            var searchIndexKeys = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.FileName).Ascending(x => x.RelativePath).Ascending(x => x.RootFolder);
            var searchIndexModel = new CreateIndexModel<FileInfoModel>(searchIndexKeys, new CreateIndexOptions { Unique = false });

            var fileNameAndRootAndTypeKeys = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.FileName).Ascending(x => x.RootFolder).Ascending(x => x.Type);
            var fileNameAndRootAndTypeModel = new CreateIndexModel<FileInfoModel>(fileNameAndRootAndTypeKeys, new CreateIndexOptions { Unique = false });

            var nameAndRelativeAndRootKeys = Builders<FileInfoModel>.IndexKeys.Text(x => x.FileName).Ascending(x => x.RelativePath).Ascending(x => x.RootFolder).Ascending(x => x.Type);
            var nameAndRelativeAndRootIndexModel = new CreateIndexModel<FileInfoModel>(nameAndRelativeAndRootKeys, new CreateIndexOptions { Unique = false });

            var idAndTypeIndexKeys = Builders<FileInfoModel>.IndexKeys.Ascending(x => x.Id).Ascending(x => x.Type);
            var idAndTypeIndexIndexModel = new CreateIndexModel<FileInfoModel>(idAndTypeIndexKeys, new CreateIndexOptions { Unique = true });


            await _fileDataDb.Indexes.CreateManyAsync([rootFolderIndexModel, rootFolderAndCreateDateIndexModel, searchIndexModel, absolutePathIndexModel, nameAndRelativeAndRootIndexModel, idAndTypeIndexIndexModel, createDateIndexModel, createDateAndTypeIndexModel, rootAndTypeIndexModel, fileNameAndRootAndTypeModel], cancellationToken);

            logger.LogInformation(@"[Init] File info data layer");

            var metaKey = Builders<FileMetadataModel>.IndexKeys.Ascending(x => x.ThumbnailAbsolutePath);
            var metaIndexModel = new CreateIndexModel<FileMetadataModel>(metaKey, new CreateIndexOptions { Unique = true });
            await _fileMetaDataDataDb.Indexes.CreateOneAsync(metaIndexModel, cancellationToken: cancellationToken);

            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            logger.LogError(ex, null);
            return (false, ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return _fileDataDb.CountDocumentsAsync(FilterDefinition<FileInfoModel>.Empty, cancellationToken: cancellationToken);
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
            return _fileDataDb.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(0L);
        }
    }


    public IAsyncEnumerable<FileInfoModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(FilterDefinition<FileInfoModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FileInfoModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<FileInfoModel> FindProjectAsync(string keyWord, int limit = 10, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        var filter = Builders<FileInfoModel>.Filter.Where(f => f.FileName.Contains(keyWord));
        // Build projection
        ProjectionDefinition<FileInfoModel>? projection = fieldsToFetch.ProjectionBuilder();

        // Fetch the documents from the database
        var options = new FindOptions<FileInfoModel, FileInfoModel>
        {
            Projection = projection
        };

        var cursor = await _fileDataDb.FindAsync(filter, options, cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var document in cursor.Current)
            {
                yield return document;
            }
        }
    }

    public async IAsyncEnumerable<FileInfoModel> Where(Expression<Func<FileInfoModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FileInfoModel, object>>[] fieldsToFetch)
    {
        var options = new FindOptions<FileInfoModel, FileInfoModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder()
        };
        var cursor = await _fileDataDb.FindAsync(predicate, options: options, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public FileInfoModel? Get(string key)
    {
        FilterDefinition<FileInfoModel> filter = ObjectId.TryParse(key, out var id) ? Builders<FileInfoModel>.Filter.Eq(x => x.Id, id) : Builders<FileInfoModel>.Filter.Eq(x => x.AbsolutePath, key);
        return _fileDataDb.Find(filter).Limit(1).FirstOrDefault();
    }

    public IAsyncEnumerable<FileInfoModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FileInfoModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<FileInfoModel> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = Builders<FileInfoModel>.Filter.Empty;
        var cursor = await _fileDataDb.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FileInfoModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            ObjectId.TryParse(key, out var id);
            var filter = Builders<FileInfoModel>.Filter.Eq(f => f.Id, id);

            // Build the update definition by combining multiple updates
            var updateDefinitionBuilder = Builders<FileInfoModel>.Update;
            var updateDefinitions = new List<UpdateDefinition<FileInfoModel>>();

            if (updates.Any())
            {
                foreach (var update in updates)
                {
                    var fieldName = update.Key;
                    var fieldValue = update.Value;

                    // Add the field-specific update to the list
                    updateDefinitions.Add(updateDefinitionBuilder.Set(fieldName, fieldValue));
                }

                // Combine all update definitions into one
                var combinedUpdate = updateDefinitionBuilder.Combine(updateDefinitions);

                await _fileDataDb.UpdateOneAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
            }

            return (true, AppLang.Update_successfully);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return (false, string.Empty);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool, string)> CreateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.Id, model.Id);
            var isExist = await _fileDataDb.Find(filter).AnyAsync(cancellationToken: cancellationToken);
            if (!isExist)
            {
                model.CreatedDate = DateTime.UtcNow.Date;
                model.ModifiedTime = DateTime.UtcNow;
                await _fileDataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
                return (true, AppLang.Create_successfully);
            }
            else
            {
                return (false, AppLang.File_is_already_exsists);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, null);
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FileInfoModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(FileInfoModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var isExists = await _fileDataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
            if (!isExists)
            {
                return (false, AppLang.File_could_not_be_found);
            }
            else
            {
                model.ModifiedTime = DateTime.UtcNow.Date;
                var filter = Builders<FileInfoModel>.Filter.Eq(x => x.Id, model.Id);
                await _fileDataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationToken);
                return (true, AppLang.Create_successfully);
            }
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
        finally
        {
            _semaphore.Release();
        }
    }


    public async IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FileInfoModel> models, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var file in models.TakeWhile(_ => cancellationToken is not { IsCancellationRequested: true }))
        {
            var result = await UpdateAsync(file, cancellationToken);
            yield return (true, result.Item2, file.Id.ToString());
        }

        yield return (true, AppLang.Success, string.Empty);
    }

    public (bool, string) Delete(string key)
    {
        _semaphore.WaitAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return (false, AppLang.File_could_not_be_found);

            var query = Get(key);
            if (query == null) return (false, AppLang.File_not_found_);

            var filter = Builders<FileInfoModel>.Filter.Eq(x => x.AbsolutePath, key);
            if (ObjectId.TryParse(key, out var id)) filter |= Builders<FileInfoModel>.Filter.Eq(x => x.Id, id);

            _fileDataDb.DeleteMany(filter);

            try
            {
                File.Delete(query.AbsolutePath);
            }
            catch (Exception e)
            {
                logger.LogError(e, null);
            }

            if (!string.IsNullOrEmpty(query.Thumbnail))
            {
                Delete(query.Thumbnail);
            }

            DeleteMetadata(query.MetadataId);

            foreach (var extend in query.ExtendResource) Delete(extend.Id);

            return (true, AppLang.Delete_successfully);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, null);
            return (false, ex.Message);
        }
        finally
        {
            _semaphore.Release();
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
            var cursor = await _fileDataDb.FindAsync(filter, options, cancellationToken: cancellationToken);
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

        if (!hasIdCheck && memoryCache.TryGetValue<ObjectId>(SearchIndexNameLastSeenId, out var cachedLastSeenId))
        {
            lastSeenId = cachedLastSeenId;
        }

        var options = new FindOptions<FileInfoModel, FileInfoModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder(),
            Limit = pageSize,
            Skip = lastSeenId == null ? pageSize * pageNumber : 0,
        };

        var filterBuilder = Builders<FileInfoModel>.Filter;
        var filter = Builders<FileInfoModel>.Filter.Empty;

        filter = lastSeenId.HasValue ? filterBuilder.And(filter, filterBuilder.Lt(f => f.Id, lastSeenId.Value)) : Builders<FileInfoModel>.Filter.Where(predicate);

        var cursor = await _fileDataDb.FindAsync(filter, options, cancellationToken: cancellationToken);

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
            memoryCache.Set(SearchIndexNameLastSeenId, currentLastSeenId.Value, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
        }
    }
}