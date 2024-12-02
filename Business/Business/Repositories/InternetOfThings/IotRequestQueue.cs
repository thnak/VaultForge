using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Business.Business.Interfaces.InternetOfThings;
using Business.Services.Configure;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Business.SignalRHub.System.Implement;
using Business.SignalRHub.System.Interfaces;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.SignalR;

namespace Business.Business.Repositories.InternetOfThings;

public class IotRequestQueue : IIotRequestQueue
{
    private readonly Channel<IoTRecord> _channel;
    private long _dailyRequestCount; // Total requests for the day

    /// <summary>
    /// Per-sensor request counts
    /// </summary>
    private readonly ConcurrentDictionary<string, ulong> _sensorRequestCounts;

    private readonly ConcurrentDictionary<string, float> _sensorLastValues;
    private readonly Lock _counterLock = new(); // Lock for resetting daily counter
    private readonly IHubContext<IoTSensorSignalHub, IIoTSensorSignal> _hub;
    private readonly IParallelBackgroundTaskQueue _backgroundTaskQueue;

    public IotRequestQueue(ApplicationConfiguration options, IHubContext<IoTSensorSignalHub, IIoTSensorSignal> hubContext, IParallelBackgroundTaskQueue backgroundTaskQueue)
    {
        var maxQueueSize = options.GetIoTRequestQueueConfig.MaxQueueSize;
        BoundedChannelOptions boundedChannelOptions = new(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        };
        _sensorRequestCounts = [];
        _sensorLastValues = [];
        _channel = Channel.CreateBounded<IoTRecord>(boundedChannelOptions);
        _hub = hubContext;
        _backgroundTaskQueue = backgroundTaskQueue;
    }

    public async Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _channel.Writer.WaitToWriteAsync(cancellationToken) && _channel.Writer.TryWrite(data);
            IncrementDailyRequestCount();
            await IncrementSensorRequestCount(data.SensorId, cancellationToken);
            await UpdateSensorLastValue(data.SensorId, data.SensorData, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public async ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }

    public void Reset()
    {
        _sensorRequestCounts.Clear();
        lock (_counterLock)
        {
            _dailyRequestCount = 0;
        }
    }

    public bool TryRead([MaybeNullWhen(false)] out IoTRecord item)
    {
        return _channel.Reader.TryRead(out item);
    }

    public long GetTotalRequests()
    {
        lock (_counterLock)
        {
            return _dailyRequestCount;
        }
    }

    public ulong GetTotalRequests(string deviceId)
    {
        _sensorRequestCounts.TryGetValue(deviceId, out var count);
        return count;
    }

    public float GetLastRecord(string deviceId)
    {
        _sensorLastValues.TryGetValue(deviceId, out var value);
        return value;
    }

    public void SetTotalRequests(string deviceId, ulong totalRequests)
    {
        _sensorRequestCounts.AddOrUpdate(deviceId, totalRequests, (_, _) => totalRequests);
    }

    public void IncrementTotalRequests(string deviceId)
    {
        _sensorRequestCounts.AddOrUpdate(deviceId, 0, (_, oldValue) => oldValue + 1);
    }


    private void IncrementDailyRequestCount()
    {
        lock (_counterLock)
        {
            // Increment the counter
            _dailyRequestCount++;
        }
    }

    /// <summary>
    /// If the sensorId doesn't exist, initialize with 1. If it exists, increment the count
    /// </summary>
    /// <param name="sensorId"></param>
    /// <param name="cancellationToken"></param>
    private async Task IncrementSensorRequestCount(string sensorId, CancellationToken cancellationToken = default)
    {
        _sensorRequestCounts.AddOrUpdate(sensorId, 1, (_, oldValue) => oldValue + 1);
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async _ =>
        {
            if (_sensorRequestCounts.TryGetValue(sensorId, out var count))
                await _hub.Clients.Groups(sensorId).ReceiveCount(count);
        }, cancellationToken);
    }

    private async Task UpdateSensorLastValue(string sensorId, float value, CancellationToken cancellationToken = default)
    {
        _sensorLastValues.AddOrUpdate(sensorId, value, (_, _) => value);
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async _ =>
        {
            if (_sensorLastValues.TryGetValue(sensorId, out var count))
                await _hub.Clients.Groups(sensorId).ReceiveValue(count);
        }, cancellationToken);
    }
}