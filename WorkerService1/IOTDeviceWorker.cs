using System.Text;
using MQTTnet;

namespace WorkerService1;

public class IotDeviceWorker(ILogger<IotDeviceWorker> logger, TimeProvider timeProvider) : BackgroundService
{
    private IMqttClient? _mqttClient;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IoT Device Worker is starting.");

        var mqttFactory = new MqttClientFactory();
        _mqttClient = mqttFactory.CreateMqttClient();

        _mqttClient.ConnectedAsync += MqttClientOnConnectedAsync;
        _mqttClient.DisconnectedAsync += MqttClientOnDisconnectedAsync;
        _mqttClient.ApplicationMessageReceivedAsync += MqttClientOnApplicationMessageReceivedAsync;

        // Attempt to connect
        await ConnectToBroker(_mqttClient, stoppingToken);

        // Run a loop to publish data periodically
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_mqttClient.IsConnected)
            {
                var payload = $"{{ \"temperature\": {new Random().Next(20, 30)}, \"humidity\": {new Random().Next(40, 60)} }}";
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("devices/Device 01/telemetry")
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                await _mqttClient.PublishAsync(message, stoppingToken);
                logger.LogInformation("Published telemetry: {Payload}", payload);
                continue;
            }

            await ConnectToBroker(_mqttClient, stoppingToken);
        }
    }

    private async Task ConnectToBroker(IMqttClient mqttClient, CancellationToken cancellationToken = default)
    {
        // Initialize the MQTT client
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithClientId(Guid.NewGuid().ToString()) // Unique client ID
            .WithTcpServer("thnakdevserver.ddns.net", 1883) // Replace with your broker's address
            .WithCredentials("Device 01", "Hi") // Replace with valid credentials
            .Build();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10), timeProvider);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                logger.LogInformation("Connecting to MQTT broker...");
                await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
                break; // Exit the loop if connected successfully
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to MQTT broker. Retrying in 5 seconds...");
            }
        }
    }

    private Task MqttClientOnConnectedAsync(MqttClientConnectedEventArgs args)
    {
        logger.LogInformation("Connected to MQTT broker.");
        return _mqttClient?.SubscribeAsync("devices/Device 01/commands") ?? Task.CompletedTask;
    }

    private Task MqttClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        logger.LogWarning("Disconnected from MQTT broker.");
        return Task.CompletedTask;
    }

    private Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

        logger.LogInformation("Received message on topic {Topic}: {Payload}", topic, payload);

        // Example: Handle a "turn_on" command
        if (topic == "devices/Device 01/commands" && payload == "turn_on")
        {
            logger.LogInformation("Turning on the device...");
            // Add actual logic to turn on the device
        }

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient != null)
        {
            await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
            logger.LogInformation("Disconnected MQTT client.");
        }

        await base.StopAsync(cancellationToken);
    }
}