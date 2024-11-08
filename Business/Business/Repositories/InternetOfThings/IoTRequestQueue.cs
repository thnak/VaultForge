using System.Collections.Concurrent;
using System.Threading.Channels;
using Business.Business.Interfaces.InternetOfThings;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.Results;
using BusinessModels.General.SettingModels;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Business.Repositories.InternetOfThings;

public class IoTRequestQueue : IDisposable, IAsyncDisposable
{
    private readonly Channel<IoTRecord> _channel;

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
        this.logger = logger;
        _channel = Channel.CreateBounded<IoTRecord>(boundedChannelOptions);
        _queue = queue;
        _iotBusinessLayer = iotBusinessLayer;

        _batchTimer = new Timer(InsertPeriodTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(timePeriod));
    }

    private void InsertPeriodTimerCallback(object? state)
    {
        lock (_lock)
        {
            _queue.QueueBackgroundWorkItemAsync(async serverToken =>
            {
                ConcurrentBag<IoTRecord> batch = [];
                while (await _channel.Reader.WaitToReadAsync(serverToken))
                {
                    while (_channel.Reader.TryRead(out var data))
                    {
                        batch.Add(data);
                    }
                }

                if (batch.Count == 0)
                    return;

                var result = await InsertBatchIntoDatabase(batch.ToList(), serverToken);
                if (!result.IsSuccess)
                {
                    logger.LogWarning(result.Message);
                }
            });
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
}