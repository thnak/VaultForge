using Microsoft.Extensions.Logging;

namespace BusinessModels.Logging;

public class LogEntryModel
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string LogLevel { get; set; } = string.Empty;
    public string Logger { get; set; } = string.Empty;
    public string Message { get; set; }
    public string Exception { get; set; }

    public LogEntryModel(LogLevel logLevel, string loggerName, string message, string? exception = null)
    {
        LogLevel = logLevel.ToString();
        Logger = loggerName;
        Message = message;
        Exception = exception ?? string.Empty;
    }
}