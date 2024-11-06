using BrainNet.Models.Result;
using BrainNet.Models.Vector;

namespace BrainNet.Database;

public interface IVectorDb : IDisposable, IAsyncDisposable
{
    public Task AddNewRecordAsync(VectorRecord vectorRecord, CancellationToken cancellationToken = default);
    public Task AddNewRecordAsync(IReadOnlyCollection<VectorRecord> vectorRecords, CancellationToken cancellationToken = default);
    
    public Task DeleteRecordAsync(int key, CancellationToken cancellationToken = default);
    public Task DeleteRecordAsync(IReadOnlyCollection<int> keys, CancellationToken cancellationToken = default);
    public Task<float[]> GenerateVectorsFromDescription(string description, CancellationToken cancellationToken = default);
    public Task<string> GenerateImageDescription(MemoryStream stream, CancellationToken cancellationToken = default);
    
    public IAsyncEnumerable<SearchScore<VectorRecord>> Search(string query, int count, CancellationToken cancellationToken = default);
    public IAsyncEnumerable<SearchScore<VectorRecord>> Search(ReadOnlyMemory<float> vector, int count, CancellationToken cancellationToken = default);
    public Task Init();
}