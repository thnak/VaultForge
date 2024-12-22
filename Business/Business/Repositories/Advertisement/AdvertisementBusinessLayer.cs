using System.Linq.Expressions;
using Business.Business.Interfaces.Advertisement;
using Business.Data.Interfaces.Advertisement;
using Business.Models;
using BusinessModels.Advertisement;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using MongoDB.Driver;

namespace Business.Business.Repositories.Advertisement;

public class AdvertisementBusinessLayer(IAdvertisementDataLayer dataLayer) : IAdvertisementBusinessLayer
{
    public Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(cancellationToken);
    }

    public Task<long> GetDocumentSizeAsync(Expression<Func<ArticleModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetDocumentSizeAsync(predicate, cancellationToken);
    }

    public IAsyncEnumerable<ArticleModel> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default)
    {
        return dataLayer.Search(queryString, limit, cancellationToken);
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(FilterDefinition<ArticleModel> filter, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(filter, cancellationToken);
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(string keyWord, CancellationToken cancellationToken = default)
    {
        return dataLayer.FindAsync(keyWord, cancellationToken);
    }

    public IAsyncEnumerable<ArticleModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        return dataLayer.FindProjectAsync(keyWord, limit, cancellationToken, fieldsToFetch);
    }

    public IAsyncEnumerable<ArticleModel> Where(Expression<Func<ArticleModel, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        return dataLayer.WhereAsync(predicate, cancellationToken, fieldsToFetch);
    }

    public ArticleModel? Get(string key)
    {
        return dataLayer.Get(key);
    }

    public Task<Result<ArticleModel?>> Get(string key, params Expression<Func<ArticleModel, object>>[] fieldsToFetch)
    {
        return dataLayer.Get(key, fieldsToFetch);
    }

    public IAsyncEnumerable<ArticleModel?> GetAsync(List<string> keys, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(ArticleModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default)
    {
        return dataLayer.GetAllAsync(page, size, cancellationToken);
    }

    public IAsyncEnumerable<ArticleModel> GetAllAsync(Expression<Func<ArticleModel, object>>[] field2Fetch, CancellationToken cancellationToken)
    {
        return dataLayer.GetAllAsync(field2Fetch, cancellationToken);
    }


    public Task<(bool, string)> UpdateAsync(string key, FieldUpdate<ArticleModel> updates, CancellationToken cancellationToken = default)
    {
        return dataLayer.UpdateAsync(key, updates, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(ArticleModel model, CancellationToken cancellationToken = default)
    {
        return dataLayer.CreateAsync(model, cancellationToken);
    }

    public Task<Result<bool>> CreateAsync(IReadOnlyCollection<ArticleModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(ArticleModel model, CancellationToken cancellationToken = default)
    {
        return dataLayer.ReplaceAsync(model, cancellationToken);
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<ArticleModel> models, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default)
    {
        throw new NotImplementedException();
    }

    public ArticleModel? Get(string title, string lang)
    {
        return dataLayer.Get(title, lang);
    }
}