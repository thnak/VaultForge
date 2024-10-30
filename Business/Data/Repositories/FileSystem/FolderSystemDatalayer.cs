using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using Business.Utils;
using Business.Utils.ExpressionExtensions;
using BusinessModels.General.EnumModel;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Data.Repositories.FileSystem;

public class FolderSystemDatalayer(IMongoDataLayerContext context, ILogger<FolderSystemDatalayer> logger, IMemoryCache memoryCache) : IFolderSystemDatalayer
{
    private readonly IMongoCollection<FolderInfoModel> _dataDb = context.MongoDatabase.GetCollection<FolderInfoModel>("FolderInfo");
    private readonly SemaphoreSlim _semaphore = new(100, 1000);
    private const string SearchIndexNameLastSeenId = "FolderInfoSearchIndexLastSeenId";

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var keys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath).Ascending(x => x.OwnerUsername);
            var absolutePathAndUserIndexModel = new CreateIndexModel<FolderInfoModel>(keys, new CreateIndexOptions { Unique = true });

            var absKeys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath);
            var absolutePathIndexModel = new CreateIndexModel<FolderInfoModel>(absKeys, new CreateIndexOptions { Unique = false });

            var createDateKey = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.CreateDate);
            var createDateIndexModel = new CreateIndexModel<FolderInfoModel>(createDateKey, new CreateIndexOptions { Unique = false });

            var createDateAndTypeKey = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.CreateDate).Ascending(x => x.Type);
            var createDateIndexAndTypeModel = new CreateIndexModel<FolderInfoModel>(createDateAndTypeKey, new CreateIndexOptions { Unique = false });

            var absAndTypeKey = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath).Ascending(x => x.Type);
            var absAndTypeIndexModel = new CreateIndexModel<FolderInfoModel>(absAndTypeKey, new CreateIndexOptions { Unique = false });

            var rootFolderIdKey = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder);
            var rootFolderIdIndexModel = new CreateIndexModel<FolderInfoModel>(rootFolderIdKey, new CreateIndexOptions { Unique = false });

            var relativePathAndUserKey = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RelativePath).Ascending(x => x.OwnerUsername);
            var relativePathAndUserIndexModel = new CreateIndexModel<FolderInfoModel>(relativePathAndUserKey, new CreateIndexOptions { Unique = true });

            var rootFolderIdAndTypeKey = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.Type);
            var rootFolderIdIndexAndTypeModel = new CreateIndexModel<FolderInfoModel>(rootFolderIdAndTypeKey, new CreateIndexOptions { Unique = false });

            var rootFolderAndFolderName = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.FolderName);
            var rootFolderAndFolderIndexModel = new CreateIndexModel<FolderInfoModel>(rootFolderAndFolderName, new CreateIndexOptions { Unique = false });

            var searchIndexKeys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.FolderName).Ascending(x => x.Type);
            var searchIndexModel = new CreateIndexModel<FolderInfoModel>(searchIndexKeys, new CreateIndexOptions() { Unique = false });

            var typeIndexKeys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.Type);
            var typeIndexModel = new CreateIndexModel<FolderInfoModel>(typeIndexKeys, new CreateIndexOptions { Unique = false });

            var userIndexKeys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.OwnerUsername);
            var userIndexModel = new CreateIndexModel<FolderInfoModel>(userIndexKeys, new CreateIndexOptions { Unique = false });

            var userAndTypeIndexKeys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.OwnerUsername).Ascending(x => x.Type);
            var userAndTypeIndexModel = new CreateIndexModel<FolderInfoModel>(userAndTypeIndexKeys, new CreateIndexOptions { Unique = false });

            List<CreateIndexModel<FolderInfoModel>> indexes =
            [
                absolutePathAndUserIndexModel, absolutePathIndexModel, rootFolderIdIndexAndTypeModel,
                searchIndexModel, rootFolderIdIndexModel, rootFolderAndFolderIndexModel,
                absAndTypeIndexModel, createDateIndexModel, createDateIndexAndTypeModel,
                relativePathAndUserIndexModel, typeIndexModel,
                userAndTypeIndexModel, userIndexModel,
            ];

            await _dataDb.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);


            var anonymousUser = "Anonymous".ComputeSha256Hash();
            var anonymousFolder = Get(anonymousUser, "/root");
            if (anonymousFolder == default)
            {
                anonymousFolder = new FolderInfoModel()
                {
                    OwnerUsername = anonymousUser,
                    RelativePath = "/root",
                    AbsolutePath = "/root",
                    FolderName = "Home",
                    Type = FolderContentType.SystemFolder
                };
                var result = await CreateAsync(anonymousFolder, cancellationToken);
                logger.LogInformation($"[Init][Anonymous] {result}");
            }

            var wallPaperFolder = Get(anonymousUser, "/root/wallpaper");
            if (wallPaperFolder == default)
            {
                wallPaperFolder = new FolderInfoModel()
                {
                    OwnerUsername = anonymousUser,
                    RootFolder = anonymousFolder.Id.ToString(),
                    FolderName = "WallPaper",
                    AbsolutePath = anonymousFolder.AbsolutePath + "/wallpaper",
                    RelativePath = anonymousFolder.AbsolutePath + "/WallPaper",
                    Type = FolderContentType.SystemFolder
                };
                var result = await CreateAsync(wallPaperFolder, cancellationToken);
                logger.LogInformation($"[Init][WallPaper] {result.Message}");
            }

            var videoFolder = Get(anonymousUser, "/root/Videos");
            if (videoFolder == default)
            {
                videoFolder = new FolderInfoModel()
                {
                    OwnerUsername = anonymousUser,
                    RootFolder = anonymousFolder.Id.ToString(),
                    FolderName = "Videos",
                    AbsolutePath = anonymousFolder.AbsolutePath + "/Videos",
                    RelativePath = anonymousFolder.AbsolutePath + "/Videos",
                    Type = FolderContentType.SystemFolder
                };
                var result = await CreateAsync(videoFolder, cancellationToken);
                logger.LogInformation($"[Init][Videos] {result.Message}");
            }


            var resourceFolder = Get(anonymousUser, "/root/wallpaper");
            if (resourceFolder == default)
            {
                resourceFolder = new FolderInfoModel()
                {
                    OwnerUsername = anonymousUser,
                    RootFolder = anonymousFolder.Id.ToString(),
                    FolderName = "resource",
                    AbsolutePath = anonymousFolder.AbsolutePath + "/resource",
                    RelativePath = anonymousFolder.AbsolutePath + "/resource",
                    Type = FolderContentType.SystemFolder
                };
                var result = await CreateAsync(resourceFolder, cancellationToken);
                logger.LogInformation($"[Init][resource] {result.Message}");
            }


            logger.LogInformation(@"[Init] Folder info data layer");
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

    public FolderInfoModel? Get(string username, string absolute, bool hashed = true)
    {
        username = hashed ? username : username.ComputeSha256Hash();
        var filter = Builders<FolderInfoModel>.Filter.Where(x => x.AbsolutePath == absolute && x.OwnerUsername == username);
        filter |= Builders<FolderInfoModel>.Filter.Where(x => x.RelativePath == absolute && x.OwnerUsername == username);
        return _dataDb.Find(filter).FirstOrDefault();
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return _dataDb.CountDocumentsAsync(filter: Builders<FolderInfoModel>.Filter.Empty, cancellationToken: cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<FolderInfoModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _dataDb.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<FolderInfoModel> Search(string queryString, int limit = 10, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var searchStage = new BsonDocument
        {
            {
                "$search", new BsonDocument
                {
                    {
                        "text", new BsonDocument
                        {
                            {
                                "query", queryString
                            }, // Specify the search term
                            {
                                "path", new BsonArray
                                {
                                    nameof(FolderInfoModel.FolderName),
                                    nameof(FolderInfoModel.RelativePath)
                                }
                            } // Specify the fields to search
                        }
                    }
                }
            }
        };
        var pipeline = new[]
        {
            searchStage,
            new()
            {
                {
                    "$limit", limit
                }
            } // Limit the number of results
        };
        var searchResults = await _dataDb.AggregateAsync<FolderInfoModel>(pipeline, null, cancellationToken);
        while (await searchResults.MoveNextAsync(cancellationToken))
            foreach (var user in searchResults.Current)
                if (user != default)
                    yield return user;
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(FilterDefinition<FolderInfoModel> filter,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindAsync(string keyWord,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<FolderInfoModel> FindProjectAsync(string keyWord, int limit = 10,
        CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }


    public async IAsyncEnumerable<FolderInfoModel> Where(Expression<Func<FolderInfoModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        var options = fieldsToFetch.Any() ? new FindOptions<FolderInfoModel, FolderInfoModel> { Projection = fieldsToFetch.ProjectionBuilder() } : null;
        using var cursor = await _dataDb.FindAsync(predicate, options, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                if (model != default)
                    yield return model;
            }
        }
    }

    public FolderInfoModel? Get(string key)
    {
        FilterDefinition<FolderInfoModel> filter = ObjectId.TryParse(key, out var id) ? Builders<FolderInfoModel>.Filter.Eq(x => x.Id, id) : Builders<FolderInfoModel>.Filter.Eq(x => x.AbsolutePath, key);
        return _dataDb.Find(filter).Limit(1).FirstOrDefault();
    }

    public IAsyncEnumerable<FolderInfoModel?> GetAsync(List<string> keys,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(FolderInfoModel[], long)> GetAllAsync(int page, int size,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<FolderInfoModel> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = Builders<FolderInfoModel>.Filter.Empty;
        using var cursor = await _dataDb.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }


    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<FolderInfoModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (ObjectId.TryParse(key, out var id))
            {
                var filter = Builders<FolderInfoModel>.Filter.Eq(f => f.Id, id);

                // Build the update definition by combining multiple updates
                var updateDefinitionBuilder = Builders<FolderInfoModel>.Update;
                var updateDefinitions = new List<UpdateDefinition<FolderInfoModel>>();

                if (updates.Any())
                {
                    updates.Add(model => model.ModifiedTime, DateTime.UtcNow);
                    foreach (var update in updates)
                    {
                        var fieldName = update.Key;
                        var fieldValue = update.Value;

                        // Add the field-specific update to the list
                        updateDefinitions.Add(updateDefinitionBuilder.Set(fieldName, fieldValue));
                    }

                    // Combine all update definitions into one
                    var combinedUpdate = updateDefinitionBuilder.Combine(updateDefinitions);

                    await _dataDb.UpdateOneAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
                }

                return (true, AppLang.Update_successfully);
            }


            return (false, AppLang.Invalid_key);
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

    public async Task<Result<bool>> CreateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            var isExists = await _dataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
            if (isExists) return Result<bool>.Failure(AppLang.Folder_already_exists, ErrorType.Duplicate);
            model.ModifiedTime = DateTime.UtcNow;
            model.CreateTime = DateTime.UtcNow;
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Create] Operation cancelled");
            return Result<bool>.Failure("canceled", ErrorType.Cancelled);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FolderInfoModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> ReplaceAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            var isExists = await _dataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
            if (!isExists)
            {
                return (false, AppLang.Folder_could_not_be_found);
            }
            else
            {
                model.ModifiedTime = DateTime.UtcNow;
                var filter = Builders<FolderInfoModel>.Filter.Eq(x => x.Id, model.Id);
                await _dataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationToken);
                return (true, AppLang.Update_successfully);
            }
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }


    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<FolderInfoModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        if (!ObjectId.TryParse(key, out var id))
            return (false, AppLang.Invalid_key);

        var filter = Builders<FolderInfoModel>.Filter.Eq(f => f.Id, id);
        var isExists = await _dataDb.Find(filter).AnyAsync(cancellationToken: cancelToken);
        if (isExists)
        {
            var folder = Get(key)!;
            await _dataDb.DeleteOneAsync(filter, cancelToken);
            if (folder.Type == FolderContentType.SystemFolder || folder.PreviousType == FolderContentType.SystemFolder)
            {
                folder.Id = ObjectId.GenerateNewId();
                folder.Type = FolderContentType.SystemFolder;
                folder.PreviousType = FolderContentType.Folder;
                await CreateAsync(folder, cancelToken);
            }
            return (true, AppLang.Delete_successfully);
        }

        return (false, "Fail");
    }

    public (FolderInfoModel?, string) GetWithPassword(string id, string password)
    {
        var model = Get(id);
        if (model != null)
        {
            if (string.IsNullOrEmpty(password)) return (default, AppLang.Incorrect_password);
            if (model.Password == password.ComputeSha256Hash()) return (model, AppLang.Success);

            return (default, AppLang.Incorrect_password);
        }

        return (default, AppLang.Folder_could_not_be_found);
    }

    public async IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(string id, int pageNumber, int pageSize, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        if (ObjectId.TryParse(id, out ObjectId objectId))
        {
            var filter = Builders<FolderInfoModel>.Filter.Eq(x => x.Id, objectId);
            var options = new FindOptions<FolderInfoModel, FolderInfoModel>
            {
                Limit = pageSize,
                Skip = pageSize * pageNumber,
            };
            if (fieldsToFetch.Any())
                options.Projection = fieldsToFetch.ProjectionBuilder();

            using var cursor = await _dataDb.FindAsync(filter, options, cancellationToken: cancellationToken);
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

    public async IAsyncEnumerable<FolderInfoModel> GetContentFormParentFolderAsync(Expression<Func<FolderInfoModel, bool>> predicate, int pageNumber, int pageSize, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        ObjectId? lastSeenId = null;

        bool hasIdCheck = predicate.PredicateContainsIdCheck(f => f.Id);

        if (!hasIdCheck && memoryCache.TryGetValue<ObjectId>(SearchIndexNameLastSeenId, out var cachedLastSeenId))
        {
            lastSeenId = cachedLastSeenId;
        }

        var options = new FindOptions<FolderInfoModel, FolderInfoModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder(),
            Limit = pageSize,
            Skip = lastSeenId == null ? pageSize * pageNumber : 0,
        };

        var filterBuilder = Builders<FolderInfoModel>.Filter;
        var filter = Builders<FolderInfoModel>.Filter.Empty;

        filter = lastSeenId.HasValue ? filterBuilder.And(filter, filterBuilder.Lt(f => f.Id, lastSeenId.Value)) : Builders<FolderInfoModel>.Filter.Where(predicate);

        using var cursor = await _dataDb.FindAsync(filter, options, cancellationToken: cancellationToken);

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

    public (bool, string, string) CreateFolder(FolderInfoModel folderInfoModel)
    {
        throw new NotImplementedException();
    }

    public string GetParentFolder(string id)
    {
        throw new NotImplementedException();
    }

    public FolderResult OpenFolder(string id)
    {
        throw new NotImplementedException();
    }

    public (bool, string) ChangeFolderPassword(string id, string password)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}