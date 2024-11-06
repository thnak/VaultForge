using BrainNet.Models.Vector;

namespace BrainNet.Database;

public interface IVectorDb : IDisposable, IAsyncDisposable
{
    public Task AddNewRecordAsync(VectorRecord vectorRecord, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<VectorRecord> Search(string query, CancellationToken cancellationToken = default);
    public Task Init();
}