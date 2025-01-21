using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.User;
using Business.Utils;
using Business.Utils.ExpressionExtensions;
using Business.Utils.Protector;
using Business.Utils.StringExtensions;
using BusinessModels.General.EnumModel;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.FileSystem;

public class FolderSystemDatalayer(IMongoDataLayerContext context, ILogger<FolderSystemDatalayer> logger, IUserDataLayer userDataLayer, TimeProvider timeProvider, IMemoryCache memoryCache)
    : IFolderSystemDatalayer
{
    private readonly IMongoCollection<FolderInfoModel> _dataDb = context.MongoDatabase.GetCollection<FolderInfoModel>("FolderInfo");

    public async Task<Result<bool>> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IndexKeysDefinition<FolderInfoModel>[] indexKeysDefinitions =
            [
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath).Ascending(x => x.OwnerUsername),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.CreateDate),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.CreateDate).Ascending(x => x.Type),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath).Ascending(x => x.Type),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RelativePath).Ascending(x => x.OwnerUsername),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.Type),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.FolderName),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Descending(x => x.FolderName),

                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Ascending(x => x.FolderName).Ascending(x => x.Type),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.RootFolder).Descending(x => x.FolderName).Ascending(x => x.Type),

                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.Type),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.OwnerUsername),
                Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.OwnerUsername).Ascending(x => x.Type)
            ];
            var indexModels = indexKeysDefinitions.Select(x => new CreateIndexModel<FolderInfoModel>(x));


            await _dataDb.Indexes.DropAllAsync(cancellationToken);
            await _dataDb.Indexes.CreateManyAsync(indexModels, cancellationToken: cancellationToken);

            await InitDefaultFolderForUser("", "/iotImage", cancellationToken);

            var userCtx = userDataLayer.GetAllAsync([], cancellationToken);
            await foreach (var user in userCtx)
            {
                await InitDefaultFolderForUser(user.UserName, "/root", cancellationToken);
                await InitDefaultFolderForUser(user.UserName, "/root/wallpaper", cancellationToken);
                await InitDefaultFolderForUser(user.UserName, "/root/videos", cancellationToken);
                await InitDefaultFolderForUser(user.UserName, "/root/resource", cancellationToken);
            }


            logger.LogInformation(@"[Init] Folder info data layer");
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Cancelled);
        }
        catch (MongoException ex)
        {
            logger.LogError(ex, null);
            return Result<bool>.Failure(ex.Message, ErrorType.Unknown);
        }
    }

    private async Task InitDefaultFolderForUser(string userName, string path, CancellationToken cancellationToken = default)
    {
        var anonymousFolder = Get(userName, path);
        if (anonymousFolder == null)
        {
            anonymousFolder = new FolderInfoModel()
            {
                OwnerUsername = userName,
                RelativePath = path,
                AbsolutePath = path,
                FolderName = path.Split('/').Last(),
                Type = FolderContentType.SystemFolder
            };
            var result = await CreateAsync(anonymousFolder, cancellationToken);
            logger.LogInformation($"[Init][{userName}] {result}");
        }
    }

    public FolderInfoModel? Get(string username, string absolute)
    {
        var filter = Builders<FolderInfoModel>.Filter.Where(x => x.AbsolutePath == absolute && x.OwnerUsername == username);
        filter |= Builders<FolderInfoModel>.Filter.Where(x => x.RelativePath == absolute && x.OwnerUsername == username);
        return _dataDb.Find(filter).FirstOrDefault();
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

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
        using var searchResults = await _dataDb.AggregateAsync<FolderInfoModel>(pipeline, null, cancellationToken);
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


    public async IAsyncEnumerable<FolderInfoModel> WhereAsync(Expression<Func<FolderInfoModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
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
        FilterDefinition<FolderInfoModel> filter = ObjectId.TryParse(key, out var id) ? Builders<FolderInfoModel>.Filter.Eq(x => x.Id, id) : Builders<FolderInfoModel>.Filter.Eq(x => x.AliasCode, key);
        return _dataDb.Find(filter).Limit(1).FirstOrDefault();
    }

    public async Task<Result<FolderInfoModel?>> Get(string key, params Expression<Func<FolderInfoModel, object>>[] fieldsToFetch)
    {
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            var findOptions = fieldsToFetch.Any() ? new FindOptions<FolderInfoModel, FolderInfoModel>() { Projection = fieldsToFetch.ProjectionBuilder(), Limit = 1 } : null;
            using var cursor = await _dataDb.FindAsync(x => x.Id == objectId, findOptions);
            var folder = cursor.FirstOrDefault();
            if (folder != null) return Result<FolderInfoModel?>.Success(folder);
            return Result<FolderInfoModel?>.Failure(AppLang.Article_does_not_exist, ErrorType.NotFound);
        }

        return Result<FolderInfoModel?>.Failure(AppLang.Invalid_key, ErrorType.Validation);
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

    public IAsyncEnumerable<FolderInfoModel> GetAllAsync(Expression<Func<FolderInfoModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _dataDb.GetAll(field2Fetch, cancellationToken);
    }


    public async Task<Result<bool>> UpdateAsync(string key, FieldUpdate<FolderInfoModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = ObjectId.TryParse(key, out var id) ? Builders<FolderInfoModel>.Filter.Eq(f => f.Id, id) : Builders<FolderInfoModel>.Filter.Eq(x => x.AliasCode, key);

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

            return Result<bool>.SuccessWithMessage(true, AppLang.Update_successfully);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return Result<bool>.Failure(AppLang.Cancel, ErrorType.Cancelled);
        }
    }

    public async Task<Result<bool>> CreateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var isExists = await _dataDb.Find(x => x.Id == model.Id || x.AliasCode == model.AliasCode).AnyAsync(cancellationToken: cancellationToken);
            if (isExists) return Result<bool>.Failure(AppLang.Folder_already_exists, ErrorType.Duplicate);
            model.ModifiedTime = timeProvider.UtcNow();
            model.CreateTime = timeProvider.UtcNow();
            model.AliasCode = model.Id.GenerateAliasKey(model.OwnerUsername + timeProvider.UtcNow().Ticks);
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Create] Operation cancelled");
            return Result<bool>.Failure("canceled", ErrorType.Cancelled);
        }
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<FolderInfoModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> ReplaceAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            var isExists = await _dataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
            if (!isExists)
            {
                return Result<bool>.Failure(AppLang.Folder_could_not_be_found, ErrorType.NotFound);
            }

            model.ModifiedTime = DateTime.UtcNow;
            var filter = Builders<FolderInfoModel>.Filter.Eq(x => x.Id, model.Id);
            await _dataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationToken);
            return Result<bool>.SuccessWithMessage(true, AppLang.Update_successfully);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Canceled(AppLang.Cancel);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message, ErrorType.Unknown);
        }
    }


    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<FolderInfoModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        if (!ObjectId.TryParse(key, out var id))
            return Result<bool>.Failure(AppLang.Invalid_key, ErrorType.Validation);

        var filter = Builders<FolderInfoModel>.Filter.Eq(f => f.Id, id);
        var isExists = await _dataDb.Find(filter).AnyAsync(cancellationToken: cancelToken);
        if (isExists)
        {
            var folder = Get(key)!;
            await _dataDb.DeleteOneAsync(filter, cancelToken);
            if (folder.Type == FolderContentType.SystemFolder || folder.PreviousType == FolderContentType.SystemFolder)
            {
                folder.Type = FolderContentType.SystemFolder;
                folder.PreviousType = FolderContentType.Folder;
                await CreateAsync(folder, cancelToken);
            }

            return Result<bool>.SuccessWithMessage(true, AppLang.Delete_successfully);
        }

        return Result<bool>.Failure(AppLang.NotFound, ErrorType.NotFound);
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
        var stringKey = predicate.GetCacheKey();
        if (!hasIdCheck && memoryCache.TryGetValue<ObjectId>(stringKey, out var cachedLastSeenId))
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
            memoryCache.Set(stringKey, currentLastSeenId.Value, TimeSpan.FromSeconds(10)); // Cache for 30 minutes
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
        //
    }
}