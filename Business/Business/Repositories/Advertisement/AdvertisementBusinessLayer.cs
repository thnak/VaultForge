using System.Linq.Expressions;
using Business.Business.Interfaces.Advertisement;
using BusinessModels.Advertisement;
using MongoDB.Driver;

namespace Business.Business.Repositories.Advertisement;

public class AdvertisementBusinessLayer : IAdvertisementBusinessLayer
{
    public IAsyncEnumerable<ArticleModel> Search(string queryString, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(FilterDefinition<ArticleModel> filter, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> Where(Expression<Func<ArticleModel, bool>> predicate, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ArticleModel? Get(string key)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(ArticleModel[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<ArticleModel> GetAllAsync(CancellationToken cancellationTokenSource)
    {
        throw new NotImplementedException();
    }

    public (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> CreateAsync(ArticleModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<ArticleModel> models, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public Task<(bool, string)> UpdateAsync(ArticleModel model)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<ArticleModel> models, CancellationToken cancellationTokenSource = default)
    {
        throw new NotImplementedException();
    }

    public (bool, string) Delete(string key)
    {
        throw new NotImplementedException();
    }
}