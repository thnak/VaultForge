using MongoDB.Driver;

namespace Business.Data.Interfaces
{
    public interface IMongoDataLayerContext
    {
        IMongoDatabase MongoDatabase { get; }
    }
}
