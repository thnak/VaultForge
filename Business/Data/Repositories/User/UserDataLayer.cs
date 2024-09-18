using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
using Business.Models;
using Business.Utils;
using BusinessModels.People;
using BusinessModels.Resources;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Data.Repositories.User;

public class UserDataLayer(IMongoDataLayerContext context, ILogger<UserDataLayer> logger) : IUserDataLayer
{
    private const string SearchIndexString = "UserSearchIndex";
    private readonly IMongoCollection<UserModel> _dataDb = context.MongoDatabase.GetCollection<UserModel>("User");

    private readonly SemaphoreSlim _semaphore = new(1, 1);


    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var nameKey = Builders<UserModel>.IndexKeys.Ascending(x => x.UserName);
            var indexModel = new CreateIndexModel<UserModel>(nameKey, new CreateIndexOptions { Unique = true });

            var searchIndexKeys = Builders<UserModel>.IndexKeys.Text(x => x.UserName).Text(x => x.FullName);
            var searchIndexOptions = new CreateIndexOptions
            {
                Name = SearchIndexString
            };

            var searchIndexModel = new CreateIndexModel<UserModel>(searchIndexKeys, searchIndexOptions);
            await _dataDb.Indexes.CreateManyAsync([indexModel, searchIndexModel], cancellationToken);

            var defaultUser = "System".ComputeSha256Hash();
            var system = Get(defaultUser);
            if (system == null)
            {
                var passWord = "PassWd2@";
                await CreateAsync(new UserModel
                {
                    UserName = defaultUser,
                    Password = passWord.ComputeSha256Hash(),
                    JoinDate = DateTime.UtcNow,
                    Roles = [..PolicyNamesAndRoles.System.Roles.Split(",")]
                }, cancellationToken);
            }

            defaultUser = "Anonymous".ComputeSha256Hash();
            system = Get(defaultUser);
            if (system == null)
            {
                var passWord = "PassWd2@";
                await CreateAsync(new UserModel
                {
                    UserName = defaultUser,
                    Password = passWord.ComputeSha256Hash(),
                    JoinDate = DateTime.UtcNow
                }, cancellationToken);
            }

            logger.LogInformation(@"[Init] User data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            return (false, ex.Message);
        }
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return _dataDb.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<UserModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _dataDb.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<UserModel> Search(string queryString, int limit = 10,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                                    nameof(UserModel.UserName),
                                    nameof(UserModel.FullName)
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
        var searchResults = await _dataDb.AggregateAsync<UserModel>(pipeline, null, cancellationToken);
        while (await searchResults.MoveNextAsync(cancellationToken))
            foreach (var user in searchResults.Current)
                if (user != default)
                    yield return user;
    }

    public IAsyncEnumerable<UserModel> FindAsync(FilterDefinition<UserModel> filter,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10,
        CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<UserModel> Where(Expression<Func<UserModel, bool>> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        var options = new FindOptions<UserModel, UserModel>
        {
            Projection = fieldsToFetch.ProjectionBuilder()
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

    public UserModel? Get(string key)
    {
        try
        {
            var filter = Builders<UserModel>.Filter.Eq(x => x.UserName, key.ComputeSha256Hash());
            filter |= Builders<UserModel>.Filter.Eq(x => x.UserName, key);
            var result = _dataDb.Find(filter).Limit(1).FirstOrDefault();
            return result;
        }
        catch (MongoException)
        {
            return default;
        }
    }

    public async IAsyncEnumerable<UserModel?> GetAsync(List<string> keys,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        foreach (var userName in keys.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            yield return Get(userName);

        _semaphore.Release();
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<UserModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            ObjectId.TryParse(key, out var id);
            var filter = Builders<UserModel>.Filter.Eq(f => f.ObjectId, id);

            // Build the update definition by combining multiple updates
            var updateDefinitionBuilder = Builders<UserModel>.Update;
            var updateDefinitions = new List<UpdateDefinition<UserModel>>();

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

    public async Task<(bool, string)> CreateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(model.UserName)) return (false, AppLang.User_name_is_not_valid);
            var query = await _dataDb.Find(x => x.UserName == model.UserName).AnyAsync(cancellationToken: cancellationToken);
            if (!query) return (false, AppLang.User_is_already_exists);
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return (true, AppLang.Success);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return (false, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }


    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<UserModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (Get(model.UserName) == null) return (false, AppLang.User_is_not_exists);
            var filter = Builders<UserModel>.Filter.Eq(x => x.UserName, model.UserName);
            var result = await _dataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationToken);
            if (result.IsAcknowledged) return (true, AppLang.Success);

            return (false, AppLang.User_update_failed);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return (false, string.Empty);
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


    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<UserModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return (false, AppLang.User_name_is_not_valid);

            var query = Get(key);
            if (query == null) return (false, AppLang.User_is_not_exists);
            query.Leave = DateTime.UtcNow;
            _ = UpdateAsync(query).Result;
            return (true, AppLang.Success);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public List<string> GetAllRoles(string userName)
    {
        userName = userName.ComputeSha256Hash();
        var filter = Builders<UserModel>.Filter.Eq(x => x.UserName, userName);
        var project = Builders<UserModel>.Projection.Expression(x => new UserModel
        {
            Roles = x.Roles
        });
        var data = _dataDb.Find(filter).Limit(1).Project(project).FirstOrDefault();
        return data?.Roles ?? [];
    }

    public UserModel GetAnonymous()
    {
        return Get("Anonymous")!;
    }
}