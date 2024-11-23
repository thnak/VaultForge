using System.Linq.Expressions;
using Business.Business.Interfaces.Chat;
using Business.Data.Interfaces.Chat;
using Business.Models;
using BusinessModels.General.Results;
using BusinessModels.People;
using MongoDB.Driver;

namespace Business.Business.Repositories.Chat;

public class ChatWithLlmBusinessLayer(IChatWithLlmDataLayer dataLayer) : IChatWithLlmBusinessLayer
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<ChatWithChatBotMessageModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(predicate, cancellationToken);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return dataLayer.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> FindAsync(FilterDefinition<ChatWithChatBotMessageModel> filter, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<ChatWithChatBotMessageModel, object>>[] fieldsToFetch)
    {
        return dataLayer.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> Where(Expression<Func<ChatWithChatBotMessageModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<ChatWithChatBotMessageModel, object>>[] fieldsToFetch)
    {
        return dataLayer.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public ChatWithChatBotMessageModel? Get(string key)
    {
        return dataLayer.Get(key);
    }

    public Task<Result<ChatWithChatBotMessageModel?>> Get(string key, params Expression<Func<ChatWithChatBotMessageModel, object>>[] fieldsToFetch)
    {
        return dataLayer.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
       return dataLayer.GetAsync(keys, cancellationToken);
    }

    public Task<(ChatWithChatBotMessageModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<ChatWithChatBotMessageModel> GetAllAsync(Expression<Func<ChatWithChatBotMessageModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return dataLayer.GetAllAsync(field2Fetch, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(ChatWithChatBotMessageModel model, CancellationToken cancellationToken = default)
    {
        return dataLayer.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<ChatWithChatBotMessageModel> models, CancellationToken cancellationToken = default)
    {
        return dataLayer.CreateAsync(models, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(ChatWithChatBotMessageModel model, CancellationToken cancellationToken = default)
    {
        return dataLayer.ReplaceAsync(model, cancellationToken);
    }

    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<ChatWithChatBotMessageModel> updates, CancellationToken cancellationToken = default)
    {
        return dataLayer.UpdateAsync(key, updates, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<ChatWithChatBotMessageModel> models, CancellationToken cancellationToken = default)
    {
        return dataLayer.ReplaceAsync(models, cancellationToken);
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        return dataLayer.DeleteAsync(key, cancelToken);
    }
}