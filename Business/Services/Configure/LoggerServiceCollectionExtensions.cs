using Business.LogProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Business.Services.Configure;

public static class LoggerServiceCollectionExtensions
{
    public static void AddLogger(this IServiceCollection services)
    {
        services.AddLogging(options =>
        {
            options.AddProvider<MongoDbLoggerProvider>();
            options.SetMinimumLevel(LogLevel.Debug);
        });
        services.AddHttpLogging();
    }

    public static void AddLoggerService(this ILoggingBuilder builder, ILoggerProvider provider)
    {
        builder.Services.AddSingleton(provider);
    }

    public static void AddProvider<T>(this ILoggingBuilder builder) where T : class, ILoggerProvider
    {
        builder.Services.AddSingleton<ILoggerProvider, T>();
    }

    public static void AddProvider<T>(this ILoggingBuilder builder, Func<IServiceProvider, T> factory)
        where T : class, ILoggerProvider
    {
        builder.Services.AddSingleton<ILoggerProvider, T>(factory);
    }
}