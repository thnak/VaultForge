using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.Logger;

namespace Business.LogProvider;

public class MqttNetLogger : IMqttNetLogger
{
    private readonly ILogger<MqttNetLogger> _logger;

    public MqttNetLogger(ILogger<MqttNetLogger> logger)
    {
        _logger = logger;
    }

    public void Publish(MqttNetLogLevel logLevel, string source, string message, object?[] parameters, Exception? exception)
    {
        var formattedMessage = $"{source}: {string.Format(message, parameters)}";

        switch (logLevel)
        {
            case MqttNetLogLevel.Error:
                _logger.LogError(exception, formattedMessage);
                break;

            case MqttNetLogLevel.Warning:
                _logger.LogWarning(formattedMessage);
                break;

            case MqttNetLogLevel.Info:
                _logger.LogInformation(formattedMessage);
                break;

            case MqttNetLogLevel.Verbose:
                _logger.LogDebug(formattedMessage);
                break;

            default:
                _logger.LogDebug(formattedMessage);
                break;
        }
    }

    public bool IsEnabled { get; } = true;
}