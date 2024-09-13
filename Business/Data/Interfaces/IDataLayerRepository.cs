using System.Linq.Expressions;
using Business.Models;
using MongoDB.Driver;

namespace Business.Data.Interfaces;

public interface IDataLayerRepository<T> where T : class
{
    /// <summary>
    /// Lấy số lượng document
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default);

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

    IAsyncEnumerable<T> Where(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);
    T? Get(string key);
    IAsyncEnumerable<T?> GetAsync(List<string> keys, CancellationToken cancellationToken = default);
    Task<(T[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> GetAllAsync(CancellationToken cancellationToken);
    Task<(bool, string)> UpdatePropertiesAsync(string key, FieldUpdate<T> updates , CancellationToken cancellationToken = default);
    Task<(bool, string)> CreateAsync(T model, CancellationToken cancellationToken = default);
    IAsyncEnumerable<(bool, string, string)> CreateAsync(IEnumerable<T> models, CancellationToken cancellationToken = default);
    Task<(bool, string)> UpdateAsync(T model, CancellationToken cancellationToken = default);
    IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<T> models, CancellationToken cancellationToken = default);
    (bool, string) Delete(string key);
}