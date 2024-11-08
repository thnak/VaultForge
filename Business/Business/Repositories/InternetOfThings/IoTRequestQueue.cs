using System.Collections.Concurrent;
using System.Threading.Channels;
using Business.Business.Interfaces.InternetOfThings;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.Results;
using BusinessModels.General.SettingModels;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Business.Repositories.InternetOfThings;

public class IoTRequestQueue : IHostedService, IDisposable, IAsyncDisposable
{
    private readonly Channel<IoTRecord> _channel;
    private readonly ConcurrentBag<IoTRecord> _batch;
    private readonly Timer _batchTimer;
    private readonly IParallelBackgroundTaskQueue _queue;
    private readonly IIoTBusinessLayer _iotBusinessLayer;
    private readonly Lock _lock = new();
    private ILogger<IoTRequestQueue> logger;

    public IoTRequestQueue(IOptions<AppSettings> options, IParallelBackgroundTaskQueue queue, IIoTBusinessLayer iotBusinessLayer, ILogger<IoTRequestQueue> logger)
    {
        var maxQueueSize = options.Value.IoTRequestQueueConfig.MaxQueueSize;
        var timePeriod = options.Value.IoTRequestQueueConfig.TimePeriodInSecond;
        BoundedChannelOptions boundedChannelOptions = new(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        };
        _channel = Channel.CreateBounded<IoTRecord>(boundedChannelOptions);
        _batch = new ConcurrentBag<IoTRecord>();
        _queue = queue;
        _iotBusinessLayer = iotBusinessLayer;
        this.logger = logger;
        logger.LogInformation($"Initialized IoT request queue {options.Value.IoTRequestQueueConfig.ToJson()}");
        _batchTimer = new Timer(InsertPeriodTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(timePeriod));
    }

    private void InsertPeriodTimerCallback(object? state)
    {
        lock (_lock)
        {
            List<IoTRecord> batchToInsert = [.._batch];
            if (batchToInsert.Count == 0)
                return;

            _queue.QueueBackgroundWorkItemAsync(async serverToken =>
            {
                var result = await InsertBatchIntoDatabase(batchToInsert, serverToken);
                if (!result.IsSuccess)
                {
                    logger.LogWarning(result.Message);
                }
            });
            _batch.Clear();
        }
    }

    public async Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default)
    {
        return await _channel.Writer.WaitToWriteAsync(cancellationToken) && _channel.Writer.TryWrite(data);
    }


    private Task<Result<bool>> InsertBatchIntoDatabase(List<IoTRecord> batch, CancellationToken cancellationToken = default)
    {
        return _iotBusinessLayer.CreateAsync(batch, cancellationToken);
    }

    public void Dispose()
    {
        _batchTimer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _batchTimer.DisposeAsync();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_channel.Reader.TryRead(out var data))
            {
                _batch.Add(data);
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAsync();
    }
}