using Business.Business.Interfaces.InternetOfThings;
using Business.Services.Configure;
using Business.Services.OnnxService.WaterMeter;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.HostedServices.IoT;

public class IoTRequestQueueHostedService(
    ApplicationConfiguration options,
    IIotRequestQueue iotRequestQueue,
    IIotRecordBusinessLayer iotBusinessLayer,
    IWaterMeterReaderQueue waterMeterReaderQueue,
    ILogger<IoTRequestQueueHostedService> logger,
    IParallelBackgroundTaskQueue queue) : BackgroundService
{
    private PeriodicTimer? BatchTimer { get; set; }
    private readonly int _timePeriod = options.GetIoTRequestQueueConfig.TimePeriodInSecond;
    private DateTime _currentDay = DateTime.UtcNow.Date; // Tracks the current day

    private async Task InsertPeriodTimerCallback(CancellationToken cancellationToken)
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

        await InsertBatchIntoDatabase(batch, cancellationToken);
    }

    private async Task InsertBatchIntoDatabase(IReadOnlyCollection<IoTRecord> batch, CancellationToken cancellationToken = default)
    {
        var result = await iotBusinessLayer.CreateAsync(batch, cancellationToken);
        if (result.IsSuccess)
        {
            var chunkedBatch = batch.Where(data => !string.IsNullOrEmpty(data.Metadata.ImagePath));

            foreach (var data in chunkedBatch)
            {
                await queue.QueueBackgroundWorkItemAsync(async _ => await waterMeterReaderQueue.GetWaterMeterReadingCountAsync(data, cancellationToken), cancellationToken);
            }
        }
        else
        {
            logger.LogWarning(result.Message);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BatchTimer = new PeriodicTimer(TimeSpan.FromSeconds(_timePeriod));
        while (await BatchTimer.WaitForNextTickAsync(stoppingToken))
        {
            await InsertPeriodTimerCallback(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        if (BatchTimer != null) BatchTimer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}