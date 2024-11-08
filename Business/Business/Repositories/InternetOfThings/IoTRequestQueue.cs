using System.Collections.Concurrent;
using System.Threading.Channels;
using Business.Business.Interfaces.InternetOfThings;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Options;

namespace Business.Business.Repositories.InternetOfThings;

public class IoTRequestQueue : IDisposable, IAsyncDisposable
{
    private readonly Channel<IoTRecord> _channel;
    private readonly ConcurrentBag<IoTRecord> _batch;
    private readonly Timer _batchTimer;
    private readonly IParallelBackgroundTaskQueue _queue;
    private readonly IIoTBusinessLayer _iotBusinessLayer;
    private readonly Lock _lock = new();


    public IoTRequestQueue(IOptions<AppSettings> options, IParallelBackgroundTaskQueue queue, IIoTBusinessLayer iotBusinessLayer)
    {
        var maxQueueSize = options.Value.IoTRequestQueueConfig.MaxQueueSize;
        var timePeriod = options.Value.IoTRequestQueueConfig.TimePeriodInSecond;
        _channel = Channel.CreateBounded<IoTRecord>(maxQueueSize);
        _batch = new ConcurrentBag<IoTRecord>();
        _queue = queue;
        _iotBusinessLayer = iotBusinessLayer;

        _batchTimer = new Timer(InsertPeriodTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(timePeriod));
        StartProcessing();
    }

    private void InsertPeriodTimerCallback(object? state)
    {
        lock (_lock)
        {
            List<IoTRecord> batchToInsert = [.._batch];
            if (batchToInsert.Count == 0)
                return;

            _queue.QueueBackgroundWorkItemAsync(async serverToken => { await InsertBatchIntoDatabase(batchToInsert, serverToken); });
            _batch.Clear();
        }
    }

    public async Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default)
    {
        return await _channel.Writer.WaitToWriteAsync(cancellationToken) && _channel.Writer.TryWrite(data);
    }

    private async void StartProcessing()
    {
        while (await _channel.Reader.WaitToReadAsync())
        {
            while (_channel.Reader.TryRead(out var data))
            {
                _batch.Add(data);
            }
        }
    }


    private async Task InsertBatchIntoDatabase(List<IoTRecord> batch, CancellationToken cancellationToken = default)
    {
        await _iotBusinessLayer.CreateAsync(batch, cancellationToken);
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