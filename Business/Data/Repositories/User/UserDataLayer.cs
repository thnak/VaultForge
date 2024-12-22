using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
using Business.Models;
using Business.Utils;
using Business.Utils.Protector;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using BusinessModels.People;
using BusinessModels.Resources;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using OperationCanceledException = System.OperationCanceledException;

namespace Business.Data.Repositories.User;

public class UserDataLayer(IMongoDataLayerContext context, ILogger<UserDataLayer> logger) : IUserDataLayer
{
    private const string SearchIndexString = "UserSearchIndex";
    private readonly IMongoCollection<UserModel> _dataDb = context.MongoDatabase.GetCollection<UserModel>("User");

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
                var result = await CreateAsync(new UserModel
                {
                    UserName = defaultUser,
                    Password = passWord.ComputeSha256Hash(),
                    JoinTime = DateTime.UtcNow,
                    Roles = [..PolicyNamesAndRoles.System.Roles.Split(",")]
                }, cancellationToken);
                if (!result.IsSuccess)
                    logger.LogError(result.Message);
            }

            defaultUser = "Anonymous".ComputeSha256Hash();
            system = Get(defaultUser);
            if (system == null)
            {
                var passWord = "PassWd2@";
                var result = await CreateAsync(new UserModel
                {
                    UserName = defaultUser,
                    Password = passWord.ComputeSha256Hash(),
                    JoinTime = DateTime.UtcNow
                }, cancellationToken);
                if (!result.IsSuccess)
                    logger.LogError(result.Message);
            }

            logger.LogInformation(@"[Init] User data layer");
            return (true, string.Empty);
        }
        catch (MongoException ex)
        {
            return (false, ex.Message);
        }
    }

    public event Func<string, Task>? Added;
    public event Func<string, Task>? Deleted;
    public event Func<string, Task>? Updated;

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

    public IAsyncEnumerable<UserModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<UserModel> WhereAsync(Expression<Func<UserModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        var options = fieldsToFetch.Any() ? new FindOptions<UserModel, UserModel> { Projection = fieldsToFetch.ProjectionBuilder() } : null;
        using var cursor = await _dataDb.FindAsync(predicate, options, cancellationToken: cancellationToken);
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
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (MongoException)
        {
            return default;
        }
    }

    public async Task<Result<UserModel?>> Get(string key, params Expression<Func<UserModel, object>>[] fieldsToFetch)
    {
        var findOptions = fieldsToFetch.Any() ? new FindOptions<UserModel, UserModel>() { Projection = fieldsToFetch.ProjectionBuilder(), Limit = 1 } : null;
        IAsyncCursor<UserModel>? cursor;
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            cursor = await _dataDb.FindAsync(x => x.Id == objectId, findOptions);
        }
        else
        {
            cursor = await _dataDb.FindAsync(x => x.UserName == key, findOptions);
        }

        var userModel = cursor.FirstOrDefault();
        if (userModel != null) return Result<UserModel?>.Success(userModel);
        return Result<UserModel?>.Failure(AppLang.Article_does_not_exist, ErrorType.NotFound);
    }

    public async IAsyncEnumerable<UserModel?> GetAsync(List<string> keys,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var userName in keys.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            yield return Get(userName);
    }

    public Task<(UserModel[], long)> GetAllAsync(int page, int size,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<UserModel> GetAllAsync(Expression<Func<UserModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return _dataDb.GetAll(field2Fetch, cancellationToken);
    }

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<UserModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _dataDb.UpdateAsync(key, updates, cancellationToken);
            if (result.IsSuccess)
                return (true, result.Message);

            return (false, AppLang.User_update_failed);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return (false, string.Empty);
        }
    }

    public async Task<Result<bool>> CreateAsync(UserModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.UserName)) return Result<bool>.Failure(AppLang.User_name_is_not_valid, ErrorType.Validation);
            var query = await _dataDb.Find(x => x.UserName == model.UserName).AnyAsync(cancellationToken: cancellationToken);
            if (query) return Result<bool>.Failure(AppLang.User_is_already_exists, ErrorType.NotFound);

            await _dataDb.InsertOneAsync(model, cancellationToken: cancellationToken);
            return Result<bool>.SuccessWithMessage(true, AppLang.Create_successfully);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[Update] Operation cancelled");
            return Result<bool>.Failure("Cancel", ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, ErrorType.Unknown);
        }
    }


    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<UserModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> ReplaceAsync(UserModel model, CancellationToken cancellationToken = default)
    {
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
    }


    public IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<UserModel> models,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key)) return (false, AppLang.User_name_is_not_valid);

            var query = Get(key);
            if (query == null) return (false, AppLang.User_is_not_exists);
            query.Leave = DateTime.UtcNow;
            await ReplaceAsync(query, cancelToken);
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

    public void Dispose()
    {
        //
    }
}