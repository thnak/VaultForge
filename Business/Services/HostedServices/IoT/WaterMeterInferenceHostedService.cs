using Business.Services.OnnxService.WaterMeter;
using Microsoft.Extensions.Hosting;

namespace Business.Services.HostedServices.IoT;

public class WaterMeterInferenceHostedService(IWaterMeterInferenceService waterMeterInference) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return waterMeterInference.RunAsync(stoppingToken);
    }
}