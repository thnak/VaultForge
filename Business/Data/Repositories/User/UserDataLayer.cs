using System.Linq.Expressions;
using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
using BusinessModels.People;
using BusinessModels.Resources;
using MongoDB.Bson;
using MongoDB.Driver;
using Protector.Utils;

namespace Business.Data.Repositories.User;

public class UserDataLayer(IMongoDataLayerContext context) : IUserDataLayer
{
    private const string SearchIndexString = "UserSearchIndex";
    private readonly IMongoCollection<UserModel> _dataDb = context.MongoDatabase.GetCollection<UserModel>("User");

    private readonly SemaphoreSlim _semaphore = new(1, 1);


    public async Task<(bool, string)> InitializeAsync()
    {
        try
        {
            var keys = Builders<UserModel>.IndexKeys.Descending(x => x.UserName);
            var indexModel = new CreateIndexModel<UserModel>(keys);

            var searchIndexKeys = Builders<UserModel>.IndexKeys.Text(x => x.UserName).Text(x => x.FullName);
            var searchIndexOptions = new CreateIndexOptions
            {
                Name = SearchIndexString
            };

            var searchIndexModel = new CreateIndexModel<UserModel>(searchIndexKeys, searchIndexOptions);
            await _dataDb.Indexes.CreateOneAsync(searchIndexModel);
            await _dataDb.Indexes.CreateOneAsync(indexModel);

            var system = Get("System");
            if (system == null)
            {
                var passWord = "PassWd2@";
                await CreateAsync(new UserModel()
                {
                    UserName = "System",
                    Password = passWord.ComputeSha256Hash(),
                    JoinDate = DateTime.Now,
                });
            }
            
            Console.WriteLine(@"[Init] User data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            return (false, ex.Message);
        }
    }
    public Task<long> GetDocumentSizeAsync(CancellationTokenSource? cancellationTokenSource = default)
    {
        return _dataDb.EstimatedDocumentCountAsync();
    }

    public async IAsyncEnumerable<UserModel> Search(string queryString, int limit = 10, CancellationTokenSource? cancellationTokenSource = default)
    {
        var searchStage = new BsonDocument
        {
            {
                "$search", new BsonDocument
                {
                    {
                        "index", SearchIndexString
                    },// Specify the name of your search index
                    {
                        "text", new BsonDocument
                        {
                            {
                                "query", queryString
                            },// Specify the search term
                            {
                                "path", new BsonArray
                                {
                                    nameof(UserModel.UserName),
                                    nameof(UserModel.FullName)
                                }
                            }// Specify the fields to search
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
            }// Limit the number of results
        };
        var searchResults = await _dataDb.AggregateAsync<UserModel>(pipeline, null, cancellationTokenSource?.Token ?? default);
        while (await searchResults.MoveNextAsync(cancellationTokenSource?.Token ?? default))
        {
            foreach (var user in searchResults.Current)
            {
                yield return user;
            }
        }
    }
    public IAsyncEnumerable<UserModel> FindAsync(FilterDefinition<UserModel> filter, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }
    public IAsyncEnumerable<UserModel> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }
    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    public async IAsyncEnumerable<UserModel> Where(Expression<Func<UserModel, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default)
    {
        var data = await _dataDb.FindAsync(predicate, new FindOptions<UserModel, UserModel>(), cancellationTokenSource?.Token ?? default);
        while (await data.MoveNextAsync(cancellationTokenSource?.Token ?? default))
        {
            foreach (var user in data.Current)
            {
                yield return user;
            }
        }
    }
    public UserModel? Get(string key)
    {
        try
        {
            var filter = Builders<UserModel>.Filter.Eq(x => x.UserName, key);
            var result = _dataDb.Find(filter).Limit(1).FirstOrDefault();
            return result;
        }
        catch (MongoException)
        {
            return default;
        }
    }
    public async IAsyncEnumerable<UserModel?> GetAsync(List<string> keys, CancellationTokenSource? cancellationTokenSource = default)
    {
        await _semaphore.WaitAsync();
        foreach (var userName in keys.TakeWhile(_ => cancellationTokenSource is null or { IsCancellationRequested: false }))
        {
            yield return Get(userName);
        }
        _semaphore.Release();
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }
    public IAsyncEnumerable<UserModel> GetAllAsync(CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }
    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }
    public async Task<(bool, string)> CreateAsync(UserModel model)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(model.UserName)) return (false, AppLang.User_name_is_not_valid);
            var query = _dataDb.Find(x => x.UserName == model.UserName).FirstOrDefault();
            if (query != null) return (false, AppLang.User_is_already_exists);
            await _dataDb.InsertOneAsync(model);
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

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<UserModel> models, CancellationTokenSource? cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(UserModel model)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (Get(model.UserName) == null) return (false, AppLang.User_is_not_exists);
            var filter = Builders<UserModel>.Filter.Eq(field: x => x.UserName, model.UserName);
            var result = await _dataDb.ReplaceOneAsync(filter, model);
            if (result.IsAcknowledged)
            {
                return (true, AppLang.Success);
            }
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

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<UserModel> models, CancellationTokenSource? cancellationTokenSource = default)
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
            query.Leave = DateTime.Now;
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
        var filter = Builders<UserModel>.Filter.Eq(x => x.UserName, userName);
        var project = Builders<UserModel>.Projection.Expression(x => new UserModel()
        {
            Roles = x.Roles
        });
        var data = _dataDb.Find(filter).Limit(1).Project(project).FirstOrDefault();
        return data?.Roles ?? [];
    }
}