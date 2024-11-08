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

public class IoTRequestQueue : BackgroundService
{
    private readonly Channel<IoTRecord> _channel;
    private Timer? BatchTimer { get; set; }
    private readonly IParallelBackgroundTaskQueue _queue;
    private readonly IIoTBusinessLayer _iotBusinessLayer;
    private readonly Lock _lock = new();
    private readonly ILogger<IoTRequestQueue> _logger;
    private readonly int _timePeriod;

    public IoTRequestQueue(IOptions<AppSettings> options, IParallelBackgroundTaskQueue queue, IIoTBusinessLayer iotBusinessLayer, ILogger<IoTRequestQueue> logger)
    {
        var maxQueueSize = options.Value.IoTRequestQueueConfig.MaxQueueSize;
        _timePeriod = options.Value.IoTRequestQueueConfig.TimePeriodInSecond;
        BoundedChannelOptions boundedChannelOptions = new(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        };
        _channel = Channel.CreateBounded<IoTRecord>(boundedChannelOptions);
        _queue = queue;
        _iotBusinessLayer = iotBusinessLayer;
        _logger = logger;
        logger.LogInformation($"Initialized IoT request queue {options.Value.IoTRequestQueueConfig.ToJson()}");
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
                var result = await InsertBatchIntoDatabase(batch, serverToken);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning(result.Message);
                }
            });
        }
    }

    public async Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default)
    {
        return await _channel.Writer.WaitToWriteAsync(cancellationToken) && _channel.Writer.TryWrite(data);
    }


    private Task<Result<bool>> InsertBatchIntoDatabase(IReadOnlyCollection<IoTRecord> batch, CancellationToken cancellationToken = default)
    {
        return _iotBusinessLayer.CreateAsync(batch, cancellationToken);
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