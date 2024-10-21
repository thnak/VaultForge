using Business.Data.Interfaces;
using BusinessModels.Logging;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Business.LogProvider;

public class MongoDbLogger : ILogger
{
    public MongoDbLogger(IMongoDataLayerContext context, string loggerName)
    {
        var options = new CreateCollectionOptions
        {
            TimeSeriesOptions = new TimeSeriesOptions("Timestamp", "LogLevel", TimeSeriesGranularity.Seconds)
        };
        context.MongoDatabase.CreateCollection("SystemLog", options);
        _logCollection = context.MongoDatabase.GetCollection<LogEntryModel>("SystemLog");
        _logCollectionName = loggerName;
    }

    private readonly IMongoCollection<LogEntryModel> _logCollection;
    private readonly string _logCollectionName;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Create a log entry using the LogEntry model
        var logEntry = new LogEntryModel(
            logLevel,
            _logCollectionName,
            exception != null ? formatter(state, exception) : state?.ToString() ?? string.Empty,
            exception?.ToString()
        );

        // Insert the log entry into the time series collection
        _logCollection.InsertOne(logEntry);
    }
}