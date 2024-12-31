using BusinessModels.Base;

namespace Business.Data.Interfaces;

public interface IThreadSafeSearchEngine<T> : IDisposable where T : BaseModelEntry
{
    public void LoadAndIndexItems(IEnumerable<T> items);
    public Task LoadAndIndexItems(IAsyncEnumerable<T> items);
    public IEnumerable<T> Search(string query, int limit = 10);
    public void RemoveItemFromIndex(T item);
}