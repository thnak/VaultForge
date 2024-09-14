using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Models;
using Business.Utils;
using BusinessModels.General.EnumModel;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Data.Repositories.FileSystem;

public class FolderSystemDatalayer(IMongoDataLayerContext context, ILogger<FolderSystemDatalayer> logger) : IFolderSystemDatalayer
{
    private const string SearchIndexString = "FolderInfoSearchIndex";
    private readonly IMongoCollection<FolderInfoModel> _dataDb = context.MongoDatabase.GetCollection<FolderInfoModel>("FolderInfo");
    private readonly SemaphoreSlim _semaphore = new(100, 1000);


    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var keys = Builders<FolderInfoModel>.IndexKeys.Ascending(x => x.AbsolutePath).Ascending(x => x.Username);
            var indexModel = new CreateIndexModel<FolderInfoModel>(keys, new CreateIndexOptions { Unique = true });

            var searchIndexKeys = Builders<FolderInfoModel>.IndexKeys.Text(x => x.FolderName).Text(x => x.RelativePath);
            var searchIndexOptions = new CreateIndexOptions
            {
                Name = SearchIndexString
            };

            var searchIndexModel = new CreateIndexModel<FolderInfoModel>(searchIndexKeys, searchIndexOptions);
            await _dataDb.Indexes.CreateManyAsync([indexModel, searchIndexModel], cancellationToken: cancellationToken);


            var anonymousUser = "Anonymous".ComputeSha256Hash();
            var anonymousFolder = Get(anonymousUser, "/root");
            if (anonymousFolder == default)
            {
                anonymousFolder = new FolderInfoModel()
                {
                    Username = anonymousUser,
                    RelativePath = "/root",
                    AbsolutePath = "/root",
                    ModifiedDate = DateTime.Now,
                    FolderName = "Home",
                    Type = FolderContentType.SystemFolder
                };
                await CreateAsync(anonymousFolder, cancellationToken);
            }

            var wallPaperFolder = Get(anonymousUser, "/root/wallpaper");
            if (wallPaperFolder == default)
            {
                wallPaperFolder = new FolderInfoModel()
                {
                    Username = anonymousUser,
                    RootFolder = anonymousFolder.Id.ToString(),
                    FolderName = "WallPaper",
                    AbsolutePath = anonymousFolder.AbsolutePath + "/wallpaper",
                    RelativePath = anonymousFolder.AbsolutePath + "/WallPaper",
                    Type = FolderContentType.SystemFolder
                };
                await CreateAsync(wallPaperFolder, cancellationToken);
            }
            
            var resourceFolder = Get(anonymousUser, "/root/wallpaper");
            if (resourceFolder == default)
            {
                resourceFolder = new FolderInfoModel()
                {
                    Username = anonymousUser,
                    RootFolder = anonymousFolder.Id.ToString(),
                    FolderName = "resource",
                    AbsolutePath = anonymousFolder.AbsolutePath + "/resource",
                    RelativePath = anonymousFolder.AbsolutePath + "/resource",
                    Type = FolderContentType.SystemFolder
                };
                await CreateAsync(resourceFolder, cancellationToken);
            }


            logger.LogInformation(@"[Init] Folder info data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
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
        var filter = Builders<FolderInfoModel>.Filter.Where(x => x.AbsolutePath == absolute && x.Username == username);
        if (ObjectId.TryParse(absolute, out ObjectId id))
        {
            filter |= Builders<FolderInfoModel>.Filter.Eq(x => x.Id, id);
        }

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
                        "index", SearchIndexString
                    }, // Specify the name of your search index
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
        var options = new FindOptions<FolderInfoModel, FolderInfoModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder()
        };
        var cursor = await _dataDb.FindAsync(predicate, options, cancellationToken: cancellationToken);
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
        var filter = Builders<FolderInfoModel>.Filter.Eq(x => x.RelativePath, key);
        if (ObjectId.TryParse(key, out var id)) filter |= Builders<FolderInfoModel>.Filter.Eq(x => x.Id, id);

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
        var cursor = await _dataDb.FindAsync(filter, cancellationToken: cancellationToken);
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

            ObjectId.TryParse(key, out var id);
            var filter = Builders<FolderInfoModel>.Filter.Eq(f => f.Id, id);

            // Build the update definition by combining multiple updates
            var updateDefinitionBuilder = Builders<FolderInfoModel>.Update;
            var updateDefinitions = new List<UpdateDefinition<FolderInfoModel>>();

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

                await _dataDb.UpdateOneAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
            }

            return (true, AppLang.Update_successfully);
        }
        catch (OperationCanceledException e)
        {
            logger.LogError(e, null);
            return (false, e.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(bool, string)> CreateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var folder = Get(model.Id.ToString());
            if (folder != null) return (false, AppLang.Folder_already_exists);

            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return (true, AppLang.Create_successfully);
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

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<FolderInfoModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(FolderInfoModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var file = Get(model.Id.ToString());
            if (file == null)
            {
                return (false, AppLang.Folder_could_not_be_found);
            }
            else
            {
                model.ModifiedDate = DateTime.UtcNow;
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


    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<FolderInfoModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
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
                Projection = fieldsToFetch.ProjectionBuilder(),
                Limit = pageSize,
                Skip = pageSize * pageNumber,
            };
            var cursor = await _dataDb.FindAsync(filter, options, cancellationToken: cancellationToken);
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
        var options = new FindOptions<FolderInfoModel, FolderInfoModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder(),
            Limit = pageSize,
            Skip = pageSize * pageNumber,
        };
        var cursor = await _dataDb.FindAsync(predicate, options, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
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
}