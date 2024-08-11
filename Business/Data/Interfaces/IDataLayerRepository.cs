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
    Task<long> GetDocumentSizeAsync(CancellationTokenSource? cancellationTokenSource = default);

    /// <summary>
    /// Tìm kiếm với từ khóa KHÔNG biết trước
    /// </summary>
    /// <param name="queryString"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> Search(string queryString, int limit = 10, CancellationTokenSource? cancellationTokenSource = default);

    /// <summary>
    /// Tìm kiếm với từ khóa KHÔNG biết trước
    /// </summary>
    /// <param name="queryString"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>    
    IAsyncEnumerable<T> Search(string queryString, int limit = 10, CancellationToken? cancellationToken = default);
    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, CancellationTokenSource? cancellationTokenSource = default);

    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="keyWord"></param>
    /// <param name="cancellationTokenSource"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default);
    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="keyWord"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default);

    IAsyncEnumerable<T> Where(Expression<Func<T, bool>> predicate, CancellationTokenSource? cancellationTokenSource = default);
    IAsyncEnumerable<T> Where(Expression<Func<T, bool>> predicate, CancellationToken? cancellationToken = default);
    T? Get(string key);
    IAsyncEnumerable<T?> GetAsync(List<string> keys, CancellationTokenSource? cancellationTokenSource = default);
    Task<(T[], long)> GetAllAsync(int page, int size, CancellationTokenSource? cancellationTokenSource = default);
    IAsyncEnumerable<T> GetAllAsync(CancellationTokenSource cancellationTokenSource);
    (bool, string) UpdateProperties(string key, Dictionary<string, dynamic> properties);
    Task<(bool, string)> CreateAsync(T model);
    IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<T> models, CancellationTokenSource? cancellationTokenSource = default);
    Task<(bool, string)> UpdateAsync(T model);
    IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<T> models, CancellationTokenSource? cancellationTokenSource = default);
    (bool, string) Delete(string key);
}