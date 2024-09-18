using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Chat;
using Business.Models;
using Business.Utils;
using BusinessModels.People;
using BusinessModels.Resources;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories.Chat;

public class ChatWithLlmDataLayer(IMongoDataLayerContext context, ILogger<ChatWithLlmDataLayer> logger) : IChatWithLlmDataLayer
{
    private const string SearchIndexString = "MessageSearchIndex";
    private readonly IMongoCollection<ChatWithChatBotMessageModel> _dataDb = context.MongoDatabase.GetCollection<ChatWithChatBotMessageModel>("ChatWithChatBotMessage");

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var nameKey = Builders<ChatWithChatBotMessageModel>.IndexKeys.Ascending(x => x.ConversationId);
            var indexModel = new CreateIndexModel<ChatWithChatBotMessageModel>(nameKey, new CreateIndexOptions { Unique = false });

            var searchIndexKeys = Builders<ChatWithChatBotMessageModel>.IndexKeys.Text(x => x.ConversationId).Text(x => x.Content);
            var searchIndexOptions = new CreateIndexOptions
            {
                Name = SearchIndexString
            };

            var searchIndexModel = new CreateIndexModel<ChatWithChatBotMessageModel>(searchIndexKeys, searchIndexOptions);
            await _dataDb.Indexes.CreateManyAsync([indexModel, searchIndexModel], cancellationToken);

            logger.LogInformation(@"[Init] Chat conversation data layer");
            return (true, AppLang.Success);
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

    public Task<long> GetDocumentSizeAsync(Expression<Func<ChatWithChatBotMessageModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return _dataDb.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> FindAsync(FilterDefinition<ChatWithChatBotMessageModel> filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<ChatWithChatBotMessageModel, object>>[] fieldsToFetch)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<ChatWithChatBotMessageModel> Where(Expression<Func<ChatWithChatBotMessageModel, bool>> predicate, [EnumeratorCancellation] CancellationToken cancellationToken = default, params Expression<Func<ChatWithChatBotMessageModel, object>>[] fieldsToFetch)
    {
        var options = new FindOptions<ChatWithChatBotMessageModel, ChatWithChatBotMessageModel>
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

    public ChatWithChatBotMessageModel? Get(string key)
    {
        try
        {
            if (ObjectId.TryParse(key, out ObjectId objectId))
            {
                var filter = Builders<ChatWithChatBotMessageModel>.Filter.Eq(x => x.Id, objectId);

                var result = _dataDb.Find(filter).Limit(1).FirstOrDefault();
                return result;
            }

            return default;
        }
        catch (MongoException)
        {
            return default;
        }
    }

    public async IAsyncEnumerable<ChatWithChatBotMessageModel?> GetAsync(List<string> keys, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        foreach (var userName in keys.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            yield return Get(userName);

        _semaphore.Release();
    }

    public Task<(ChatWithChatBotMessageModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> GetAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> CreateAsync(ChatWithChatBotMessageModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var query = await _dataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
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

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<ChatWithChatBotMessageModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<(bool, string)> UpdateAsync(ChatWithChatBotMessageModel model, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var isExists = await _dataDb.Find(x => x.Id == model.Id).AnyAsync(cancellationToken: cancellationToken);
            if (!isExists)
                return (false, AppLang.User_is_not_exists);
            var filter = Builders<ChatWithChatBotMessageModel>.Filter.Eq(x => x.Id, model.Id);
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

    public async Task<(bool, string)> UpdateAsync(string key, FieldUpdate<ChatWithChatBotMessageModel> updates, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);

            if (ObjectId.TryParse(key, out var id))
            {
                var isExists = await _dataDb.Find(x => x.Id == id).AnyAsync(cancellationToken: cancellationToken);
                if (!isExists)
                {
                    return (false, AppLang.File_not_found_);
                }

                var filter = Builders<ChatWithChatBotMessageModel>.Filter.Eq(f => f.Id, id);

                // Build the update definition by combining multiple updates
                var updateDefinitionBuilder = Builders<ChatWithChatBotMessageModel>.Update;
                var updateDefinitions = new List<UpdateDefinition<ChatWithChatBotMessageModel>>();

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

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<ChatWithChatBotMessageModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }
}