using MongoDB.Driver;

namespace Business.Business.Interfaces;

public interface IBusinessLayerRepository<T> where T : class
{
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, CancellationTokenSource? cancellationTokenSource = default);
    IAsyncEnumerable<T> FindAsync(string keyWord, CancellationTokenSource? cancellationTokenSource = default);
    IAsyncEnumerable<T> FindProjectAsync(string keyWord, int limit = 10, CancellationToken? cancellationToken = default);
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