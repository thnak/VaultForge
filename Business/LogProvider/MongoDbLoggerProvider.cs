using Business.Data.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.LogProvider;

public class MongoDbLoggerProvider(IServiceProvider serviceProvider) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        var dataContext = serviceProvider.GetService<IMongoDataLayerContext>()!;
        return new MongoDbLogger(dataContext, categoryName);
    }

    public void Dispose()
    {
    }
}