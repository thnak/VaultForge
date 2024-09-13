using System.Linq.Expressions;
using MongoDB.Driver;

namespace Business.Business.Interfaces;

public interface IBusinessLayerRepository<T> where T : class
{
    IAsyncEnumerable<T> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<T> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<T> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);

    IAsyncEnumerable<T> Where(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);

    T? Get(string key);
    IAsyncEnumerable<T?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default);
    Task<(T[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<T> GetAllAsync(CancellationToken cancellationTokenSource);
    Task<(bool, string)> UpdatePropertiesAsync(string key, Dictionary<Expression<Func<T, object>>, object> updates, CancellationToken cancellationTokenSource = default);
    Task<(bool, string)> CreateAsync(T model, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<T> models, CancellationToken cancellationTokenSource = default);
    Task<(bool, string)> UpdateAsync(T model, CancellationToken cancellationToken = default);
    IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<T> models, CancellationToken cancellationToken = default);
    (bool, string) Delete(string key);
}