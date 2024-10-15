using Business.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.LogProvider;

public class MongoDbLoggerProvider(IMongoDataLayerContext dataContext) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new MongoDbLogger(dataContext, categoryName);
    }

    public void Dispose()
    {
    }
}