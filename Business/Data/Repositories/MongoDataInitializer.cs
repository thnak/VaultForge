using Business.Data.Interfaces;

namespace Business.Data.Repositories;

public class MongoDataInitializer : IMongoDataInitializer
{
    public Task<(bool, string)> InitializeAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}