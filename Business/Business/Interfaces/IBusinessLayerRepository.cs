using System.Linq.Expressions;
using MongoDB.Driver;

namespace Business.Business.Interfaces;

public interface IBusinessLayerRepository<T> where T : class
{
    IAsyncEnumerable<T> Search(string queryString, int limit = 10, CancellationToken? cancellationToken = default);
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<T> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<T> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default);

    IAsyncEnumerable<T> Where(Expression<Func<T, bool>> predicate, CancellationToken? cancellationToken = default);

    T? Get(string key);
    IAsyncEnumerable<T?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default);
    Task<(T[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<T> GetAllAsync(CancellationToken cancellationTokenSource);
    (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties);
    Task<(bool, string)> CreateAsync(T model);
    IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<T> models, CancellationToken cancellationTokenSource = default);
    Task<(bool, string)> UpdateAsync(T model);
    IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<T> models, CancellationToken cancellationTokenSource = default);
    (bool, string) Delete(string key);
}