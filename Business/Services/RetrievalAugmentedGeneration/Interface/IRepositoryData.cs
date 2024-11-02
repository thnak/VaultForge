using BusinessModels.General.Results;

namespace Business.Services.RetrievalAugmentedGeneration.Interface;

public interface IRepositoryData<T> : IBaseInitialize, IDisposable where T : class
{
    public Task AddAsync(T entity, CancellationToken cancellationToken = default);
    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    public Task<SearchResult<T>> SearchAsync(string query, CancellationToken cancellationToken = default);
}