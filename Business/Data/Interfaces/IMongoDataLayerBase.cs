using MongoDB.Driver;

namespace Business.Data.Interfaces;

public interface IMongoDataLayerContext : IDisposable
{
    IMongoDatabase MongoDatabase { get; }
}