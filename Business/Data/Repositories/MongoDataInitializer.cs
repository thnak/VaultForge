using Business.Data.Interfaces;

namespace Business.Data.Repositories;

public class MongoDataInitializer : IMongoDataInitializer
{
    public Task<(bool, string)> InitializeAsync()
    {
        throw new NotImplementedException();
    }
    
}