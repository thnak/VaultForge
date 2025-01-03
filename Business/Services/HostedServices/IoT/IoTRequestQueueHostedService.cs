using Business.Business.Interfaces.InternetOfThings;
using Business.Services.Configure;
using Business.Services.OnnxService.WaterMeter;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.HostedServices.IoT;

public class IoTRequestQueueHostedService(
    ApplicationConfiguration options,
    IIotRequestQueue iotRequestQueue,
    IIotRecordBusinessLayer iotBusinessLayer,
    IWaterMeterReaderQueue waterMeterReaderQueue,
    ILogger<IoTRequestQueueHostedService> logger) : BackgroundService
{
    private Timer? BatchTimer { get; set; }
    private readonly int _timePeriod = options.GetIoTRequestQueueConfig.TimePeriodInSecond;
    private DateTime _currentDay = DateTime.UtcNow.Date; // Tracks the current day

    private void InsertPeriodTimerCallback(object? state)
    {
        var today = DateTime.UtcNow.Date;
        // Reset the counter if the day has changed
        if (_currentDay != today)
        {
            iotRequestQueue.Reset();
            _currentDay = today;
        }

        List<IoTRecord> batch = [];
        while (iotRequestQueue.TryRead(out var data))
        {
            batch.Add(data);
        }

        if (batch.Count == 0)
            return;

        InsertBatchIntoDatabase(batch).ConfigureAwait(false);
    }

    private async Task InsertBatchIntoDatabase(IReadOnlyCollection<IoTRecord> batch, CancellationToken cancellationToken = default)
    {
        var result = await iotBusinessLayer.CreateAsync(batch, cancellationToken);
        if (result.IsSuccess)
        {
            foreach (var data in batch)
            {
                if (!string.IsNullOrEmpty(data.Metadata.ImagePath))
                {
                    await waterMeterReaderQueue.GetWaterMeterReadingCountAsync(data, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        else
        {
            logger.LogWarning(result.Message);
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BatchTimer = new Timer(InsertPeriodTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(_timePeriod));
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        if (BatchTimer != null) await BatchTimer.DisposeAsync();
        await base.StopAsync(stoppingToken);
    }
}