using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
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


    public async Task<(bool, string)> InitializeAsync()
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
            await _dataDb.Indexes.CreateManyAsync([indexModel, searchIndexModel]);

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
                });
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
                });
            }

            logger.LogInformation(@"[Init] User data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            return (false, ex.Message);
        }
    }

    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationTokenSource = default)
    {
        return _dataDb.EstimatedDocumentCountAsync(cancellationToken: cancellationTokenSource);
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
        CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<UserModel> Where(Expression<Func<UserModel, bool>> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var cursor = await _dataDb.FindAsync(predicate, cancellationToken: cancellationToken);
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
        [EnumeratorCancellation] CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
        foreach (var userName in keys.TakeWhile(_ => !cancellationTokenSource.IsCancellationRequested))
            yield return Get(userName);

        _semaphore.Release();
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size,
        CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationToken cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateAsync(UserModel model, CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
        try
        {
            if (string.IsNullOrWhiteSpace(model.UserName)) return (false, AppLang.User_name_is_not_valid);
            var query = _dataDb.Find(x => x.UserName == model.UserName).FirstOrDefault();
            if (query != null) return (false, AppLang.User_is_already_exists);
            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationTokenSource);
            return (true, AppLang.Success);
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
        CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(UserModel model, CancellationToken cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync(cancellationTokenSource);
        try
        {
            if (Get(model.UserName) == null) return (false, AppLang.User_is_not_exists);
            var filter = Builders<UserModel>.Filter.Eq(x => x.UserName, model.UserName);
            var result = await _dataDb.ReplaceOneAsync(filter, model, cancellationToken: cancellationTokenSource);
            if (result.IsAcknowledged) return (true, AppLang.Success);

            return (false, AppLang.User_update_failed);
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
        CancellationToken cancellationTokenSource = default)
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