using Business.Data.Interfaces;
using Business.Data.Repositories;

namespace Business.Utils;

public static class DatabaseExtensions
{
    public static async Task InitAsync<T>(this MongoDataInitializer initializer) where T : IMongoDataInitializer
    {
        if (Activator.CreateInstance(typeof(T), initializer) is IMongoDataInitializer service) await service.InitializeAsync();
    }
}