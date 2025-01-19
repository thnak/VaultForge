using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Business.Business.Interfaces.InternetOfThings;
using Business.LogProvider;
using Business.Services.Configure;
using BusinessModels.General.Update;
using BusinessModels.System.InternetOfThings;
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
            .WithDefaultEndpointBoundIPV6Address(IPAddress.Any); // Accept connections from any IP

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
        try
        {
            await _mqttServer.StartAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }

    private async Task MqttServerOnInterceptingSubscriptionAsync(InterceptingSubscriptionEventArgs arg)
    {
        await deviceBs.UpdateAsync(arg.UserName, new FieldUpdate<IoTDevice>()
        {
            { x => x.LastServiceDate, DateTime.Now },
            { x => x.Status, IoTDeviceStatus.Active }
        });
    }

    private async Task MqttServerOnInterceptingUnsubscriptionAsync(InterceptingUnsubscriptionEventArgs arg)
    {
        await deviceBs.UpdateLastServiceTime(arg.UserName);
    }

    private Task MqttServerOnInterceptingPublishAsync(InterceptingPublishEventArgs args)
    {
        string topic = args.ApplicationMessage.Topic;
        string payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);

        logger.LogInformation($"Topic: {topic} Payload: {payload}");

        return Task.CompletedTask;
    }


    private Task MqttServerOnStoppedAsync(EventArgs arg)
    {
        logger.LogInformation("Mqtt server stopped.");
        return Task.CompletedTask;
    }

    private Task MqttServerOnStartedAsync(EventArgs arg)
    {
        logger.LogInformation("Mqtt server started");
        return Task.CompletedTask;
    }

    private async Task MqttServerOnValidatingConnectionAsync(ValidatingConnectionEventArgs context)
    {
        var device = deviceBs.ValidateUser(context.UserName, context.Password);
        if (!device.IsSuccess)
        {
            context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
            return;
        }

        context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
        await deviceBs.UpdateAsync(context.UserName, new FieldUpdate<IoTDevice>()
        {
            { x => x.LastServiceDate, DateTime.UtcNow }
        });
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