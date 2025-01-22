using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Business.Business.Interfaces.InternetOfThings;
using Business.LogProvider;
using Business.Services.Configure;
using BusinessModels.System.InternetOfThings.status;
using Microsoft.Extensions.Logging;

namespace Business.Services.BackgroundServices.Mqtt;

using Microsoft.Extensions.Hosting;
using MQTTnet.Server;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

public class MqttBrokerHostedService(
    ApplicationConfiguration configuration,
    ILogger<MqttBrokerHostedService> logger,
    ILogger<MqttNetLogger> mqttLogger,
    IIotDeviceBusinessLayer deviceBs) : BackgroundService
{
    private MqttServer? _mqttServer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttSettings = configuration.GetMqttSettings;

        var optionsBuilder = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(mqttSettings.NonSslPort) // Standard MQTT port
            .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
            .WithDefaultEndpointBoundIPV6Address(IPAddress.IPv6Any); // Accept connections from any IP

        if (mqttSettings.EnableSsl)
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(configuration.GetAppCertificate.FilePath, configuration.GetAppCertificate.Password);
            optionsBuilder
                .WithEncryptedEndpoint()
                .WithEncryptedEndpointPort(mqttSettings.SslPort)
                .WithEncryptionCertificate(certificate)
                .WithEncryptionSslProtocol(SslProtocols.Tls13);

            logger.LogInformation($"SSL is enabled on port {mqttSettings.SslPort} using certificate at {configuration.GetAppCertificate.FilePath}");
        }

        // Create the MQTT Server options
        var mqttServerOptions = optionsBuilder.Build();

        // Create and start the MQTT Server
        _mqttServer = new MqttServerFactory(new MqttNetLogger(mqttLogger)).CreateMqttServer(mqttServerOptions);
        _mqttServer.ValidatingConnectionAsync += MqttServerOnValidatingConnectionAsync;
        _mqttServer.StartedAsync += MqttServerOnStartedAsync;
        _mqttServer.StoppedAsync += MqttServerOnStoppedAsync;
        _mqttServer.InterceptingSubscriptionAsync += MqttServerOnInterceptingSubscriptionAsync;
        _mqttServer.InterceptingUnsubscriptionAsync += MqttServerOnInterceptingUnsubscriptionAsync;
        _mqttServer.InterceptingPublishAsync += MqttServerOnInterceptingPublishAsync;
        _mqttServer.ClientConnectedAsync += MqttServerOnClientConnectedAsync;
        _mqttServer.ClientDisconnectedAsync += MqttServerOnClientDisconnectedAsync;
        try
        {
            await _mqttServer.StartAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }

    private async Task MqttServerOnClientDisconnectedAsync(ClientDisconnectedEventArgs arg)
    {
        await deviceBs.UpdateLastServiceTime(arg.UserName, IoTDeviceStatus.Offline);
    }

    private async Task MqttServerOnClientConnectedAsync(ClientConnectedEventArgs arg)
    {
        await deviceBs.UpdateLastServiceTime(arg.UserName, IoTDeviceStatus.Active);
    }

    private async Task MqttServerOnInterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs arg)
    {
        await deviceBs.UpdateLastServiceTime(arg.UserName, IoTDeviceStatus.Active);
    }

    private async Task MqttServerOnInterceptingUnsubscriptionAsync(InterceptingUnsubscriptionEventArgs arg)
    {
        await deviceBs.UpdateLastServiceTime(arg.UserName, IoTDeviceStatus.Offline);
    }

    private async Task MqttServerOnInterceptingPublishAsync(InterceptingPublishEventArgs args)
    {
        string topic = args.ApplicationMessage.Topic;
        string payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
        logger.LogInformation($"Topic: {topic} Payload: {payload}");
        await deviceBs.UpdateLastServiceTime(args.UserName, IoTDeviceStatus.Offline);
    }


    private Task MqttServerOnStoppedAsync(EventArgs arg)
    {
        return Task.CompletedTask;
    }

    private Task MqttServerOnStartedAsync(EventArgs arg)
    {
        return Task.CompletedTask;
    }

    private async Task MqttServerOnValidatingConnectionAsync(ValidatingConnectionEventArgs context)
    {
        var device = deviceBs.ValidateUser(context.UserName, context.Password);
        if (!device.IsSuccess)
        {
            context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
            logger.LogInformation(device.Message);
            return;
        }

        context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
        await deviceBs.UpdateLastServiceTime(context.UserName, IoTDeviceStatus.Active);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        // Stop the MQTT server when the application stops
        if (_mqttServer != null)
        {
            await _mqttServer.StopAsync();
        }

        await base.StopAsync(stoppingToken);
    }
}