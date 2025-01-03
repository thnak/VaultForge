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

        queue.QueueBackgroundWorkItemAsync(async serverCancel => await InsertBatchIntoDatabase(batch, serverCancel));
    }

    private async Task InsertBatchIntoDatabase(IReadOnlyCollection<IoTRecord> batch, CancellationToken cancellationToken = default)
    {
        var result = await iotBusinessLayer.CreateAsync(batch, cancellationToken);
        if (result.IsSuccess)
        {
            const int chunkSize = 32; // Adjust based on memory and processing constraints
            var chunkedBatch = batch.Where(data => !string.IsNullOrEmpty(data.Metadata.ImagePath)).Chunk(chunkSize);

            foreach (var chunk in chunkedBatch)
            {
                var tasks = chunk
                    .Select(data => Task.Run(() =>
                        waterMeterReaderQueue.GetWaterMeterReadingCountAsync(data, cancellationToken), cancellationToken));

                // Start all tasks for this chunk without awaiting
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while processing a chunk of water meter readings.");
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