using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.Logger;

namespace Business.LogProvider;

public class MqttNetLogger(ILogger<MqttNetLogger> logger) : IMqttNetLogger
{
    public void Publish(MqttNetLogLevel logLevel, string source, string message, object?[]? parameters, Exception? exception)
    {
        message = string.IsNullOrEmpty(message) ? string.Empty : message;
        var formattedMessage = $"{source}: {string.Format(message, parameters ?? [])}";

        switch (logLevel)
        {
            case MqttNetLogLevel.Error:
                logger.LogError(exception, formattedMessage);
                break;

            case MqttNetLogLevel.Warning:
                logger.LogWarning(formattedMessage);
                break;

            case MqttNetLogLevel.Info:
                logger.LogInformation(formattedMessage);
                break;

            case MqttNetLogLevel.Verbose:
                logger.LogDebug(formattedMessage);
                break;

            default:
                logger.LogDebug(formattedMessage);
                break;
        }
    }

    public bool IsEnabled => true;
}