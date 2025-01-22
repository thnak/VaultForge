using System.Threading.Channels;
using Business.Business.Interfaces.InternetOfThings;
using Business.Services.Configure;
using Business.Services.OnnxService.WaterMeter;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.HostedServices.IoT;

public class IoTRequestQueueBackgroundService(
    ApplicationConfiguration options,
    IIotRequestQueue iotRequestQueue,
    IIotRecordBusinessLayer iotRecordBusinessLayer,
    IWaterMeterReaderQueue waterMeterReaderQueue,
    ILogger<IoTRequestQueueBackgroundService> logger,
    IParallelBackgroundTaskQueue queue, TimeProvider timeProvider) : BackgroundService
{
    private PeriodicTimer? BatchTimer { get; set; }
    private readonly int _timePeriod = options.GetIoTRequestQueueConfig.TimePeriodInSecond;
    private DateTime _currentDay = DateTime.UtcNow.Date; // Tracks the current day


    private readonly Channel<IoTRecordUpdateModel> _ioTRequestQueue = Channel.CreateBounded<IoTRecordUpdateModel>(new BoundedChannelOptions(1024) { FullMode = BoundedChannelFullMode.Wait });

    private async Task InsertPeriodTimerCallback(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        // Reset the counter if the day has changed
        if (_currentDay != today)
        {
            iotRequestQueue.Reset();
            _currentDay = today;
        }

        await InsertBatchIntoDatabase(cancellationToken);
        await BulkUpdateAsync(cancellationToken);
    }

    private async Task InsertBatchIntoDatabase(CancellationToken cancellationToken = default)
    {
        List<IoTRecord> batch = [];
        while (iotRequestQueue.TryRead(out var data))
        {
            batch.Add(data);
        }

        var result = await iotRecordBusinessLayer.CreateAsync(batch, cancellationToken);
        if (result.IsSuccess)
        {
            var chunkedBatch = batch.Where(data => !string.IsNullOrEmpty(data.Metadata.ImagePath))
                .Select(async data => _ioTRequestQueue.Writer.WriteAsync(await waterMeterReaderQueue.GetWaterMeterReadingCountAsync(data, cancellationToken), cancellationToken));
            await queue.QueueBackgroundWorkItemAsync(async _ => await Task.WhenAll(chunkedBatch), cancellationToken);
        }
        else
        {
            logger.LogWarning(result.Message);
        }
    }

    private async Task BulkUpdateAsync(CancellationToken cancellationToken = default)
    {
        List<IoTRecordUpdateModel> batch = [];
        while (_ioTRequestQueue.Reader.TryRead(out var data))
            batch.Add(data);
        await queue.QueueBackgroundWorkItemAsync(async token =>
        {
            var result = await iotRecordBusinessLayer.UpdateIoTValuesBatch(batch, token);
            if (!result.IsSuccess)
            {
                logger.LogWarning(result.Message);
            }
        }, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        BatchTimer = new PeriodicTimer(TimeSpan.FromSeconds(_timePeriod), timeProvider);
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