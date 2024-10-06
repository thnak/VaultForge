using Business.Data.Interfaces;
using BusinessModels.Logging;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Business.LogProvider;

public class MongoDbLogger(IMongoDataLayerContext context, string loggerName) : ILogger
{
    private readonly IMongoCollection<LogEntryModel> _logCollection = context.MongoDatabase.GetCollection<LogEntryModel>("logs");

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        // Create a log entry using the LogEntry model
        var logEntry = new LogEntryModel(
            logLevel,
            loggerName,
            exception != null ? formatter(state, exception) : state?.ToString() ?? string.Empty,
            exception?.ToString()
        );

        // Insert the log entry into the time series collection
        _logCollection.InsertOne(logEntry);
    }
}