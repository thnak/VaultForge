using System.Linq.Expressions;
using MongoDB.Driver;

namespace Business.Data.Interfaces;

public interface IDataLayerRepository<T> where T : class
{
    /// <summary>
    /// Lấy số lượng document
    /// </summary>
    /// <param name="cancellationTokenSource"></param>
    /// <returns></returns>
    Task<long> GetDocumentSizeAsync(CancellationToken cancellationTokenSource = default);

    /// <summary>
    /// Tìm kiếm với từ khóa KHÔNG biết trước
    /// </summary>
    /// <param name="queryString"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>    
    IAsyncEnumerable<T> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, CancellationToken cancellationTokenSource = default);

    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="keyWord"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindAsync(string keyWord, CancellationToken cancellationTokenSource = default);

    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="keyWord"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="fieldsToFetch"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);

    IAsyncEnumerable<T> Where(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);
    T? Get(string key);
    IAsyncEnumerable<T?> GetAsync(List<string> keys, CancellationToken cancellationTokenSource = default);
    Task<(T[], long)> GetAllAsync(int page, int size, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<T> GetAllAsync(CancellationToken cancellationTokenSource);
    Task<(bool, string)> UpdatePropertiesAsync(string key, Dictionary<Expression<Func<T, object>>, object> updates, CancellationToken cancellationTokenSource = default);
    Task<(bool, string)> CreateAsync(T model, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<T> models, CancellationToken cancellationTokenSource = default);
    Task<(bool, string)> UpdateAsync(T model, CancellationToken cancellationTokenSource = default);
    IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<T> models, CancellationToken cancellationTokenSource = default);
    (bool, string) Delete(string key);
}