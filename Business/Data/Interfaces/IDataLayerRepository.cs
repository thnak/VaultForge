using System.Linq.Expressions;
using BusinessModels.General.Results;
using BusinessModels.General.Update;
using MongoDB.Driver;

namespace Business.Data.Interfaces;

public interface IDataLayerRepository<T> where T : class
{
    event Func<string, Task> Added;
    event Func<string, Task> Deleted;
    event Func<string, Task> Updated;

    /// <summary>
    /// Lấy số lượng document
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default);

    Task<long> GetDocumentSizeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="keyWord"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindAsync(string keyWord, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm kiếm với từ khóa biết trước
    /// </summary>
    /// <param name="keyWord"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="fieldsToFetch"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);

    IAsyncEnumerable<T> WhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);
    T? Get(string key);
    Task<Result<T?>> Get(string key, params Expression<Func<T, object>>[] fieldsToFetch);
    IAsyncEnumerable<T?> GetAsync(List<string> keys, CancellationToken cancellationToken = default);
    Task<(T[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> GetAllAsync(Expression<Func<T, object>>[] field2Fetch, CancellationToken cancellationToken);
    Task<Result<bool>> CreateAsync(T model, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreateAsync(IReadOnlyCollection<T> models, CancellationToken cancellationToken = default);
    Task<Result<bool>> ReplaceAsync(T model, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateAsync(string key, FieldUpdate<T> updates, CancellationToken cancellationToken = default);

    IAsyncEnumerable<(bool, string, string)> ReplaceAsync(IEnumerable<T> models, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(string key, CancellationToken cancelToken = default);
}