using Business.Models.Vector;

namespace Business.Data.Interfaces.VectorDb;

public interface IVectorDataLayer : IMongoDataInitializer, IDataLayerRepository<VectorRecord>
{
    public bool Exists(string collection, string id);
    public IAsyncEnumerable<VectorRecord> GetAsyncEnumerator(string collection, string id, CancellationToken cancellationToken = default);
}