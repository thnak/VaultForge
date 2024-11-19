using System.Linq.Expressions;
using Business.Models;
using BusinessModels.General.Results;
using MongoDB.Driver;

namespace Business.Business.Interfaces;

public interface IBusinessLayerRepository<T> where T : class
{
    /// <summary>
    /// Lấy số lượng document
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<long> GetDocumentSizeAsync(CancellationToken cancellationToken = default);

    Task<long> GetDocumentSizeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> Search(string queryString, int limit = 10, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> FindAsync(string keyWord, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> FindProjectAsync(string keyWord, int limit = 10, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);

    IAsyncEnumerable<T> Where(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] fieldsToFetch);

    T? Get(string key);
    Task<Result<T?>> Get(string key, params Expression<Func<T, object>>[] fieldsToFetch);
    IAsyncEnumerable<T?> GetAsync(List<string> keys, CancellationToken cancellationToken = default);
    Task<(T[], long)> GetAllAsync(int page, int size, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> GetAllAsync(Expression<Func<T, object>>[] field2Fetch, CancellationToken cancellationToken);
    Task<Result<bool>> CreateAsync(T model, CancellationToken cancellationToken = default);
    Task<Result<bool>> CreateAsync(IReadOnlyCollection<T> models, CancellationToken cancellationToken = default);
    Task<(bool, string)> UpdateAsync(T model, CancellationToken cancellationToken = default);
    Task<(bool, string)> UpdateAsync(string key, FieldUpdate<T> updates, CancellationToken cancellationToken = default);

    IAsyncEnumerable<(bool, string, string)> UpdateAsync(IEnumerable<T> models, CancellationToken cancellationToken = default);
    Task<(bool, string)> DeleteAsync(string key, CancellationToken cancelToken = default);
}