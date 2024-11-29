using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Business.Business.Interfaces.InternetOfThings;
using Business.Services.Configure;
using BusinessModels.System.InternetOfThings;

namespace Business.Business.Repositories.InternetOfThings;

public class IotRequestQueue : IIotRequestQueue
{
    private readonly Channel<IoTRecord> _channel;
    private long _dailyRequestCount; // Total requests for the day
    private readonly ConcurrentDictionary<string, long> _sensorRequestCounts; // Per-sensor request counts
    private DateTime _currentDay; // Tracks the current day
    private readonly Lock _counterLock = new(); // Lock for resetting daily counter

    public IotRequestQueue(ApplicationConfiguration options)
    {
        var maxQueueSize = options.GetIoTRequestQueueConfig.MaxQueueSize;
        BoundedChannelOptions boundedChannelOptions = new(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        };
        _sensorRequestCounts = [];
        _channel = Channel.CreateBounded<IoTRecord>(boundedChannelOptions);
    }

    public async Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _channel.Writer.WaitToWriteAsync(cancellationToken) && _channel.Writer.TryWrite(data);
            IncrementDailyRequestCount();
            IncrementSensorRequestCount(data.SensorId);
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

    public long GetTotalRequests(string deviceId)
    {
        _sensorRequestCounts.TryGetValue(deviceId, out var count);
        return count;
    }


    private void IncrementDailyRequestCount()
    {
        lock (_counterLock)
        {
            var today = DateTime.UtcNow.Date;

            // Reset the counter if the day has changed
            if (_currentDay != today)
            {
                _currentDay = today;
                _dailyRequestCount = 0;
            }

            // Increment the counter
            _dailyRequestCount++;
        }
    }

    /// <summary>
    /// If the sensorId doesn't exist, initialize with 1. If it exists, increment the count
    /// </summary>
    /// <param name="sensorId"></param>
    private void IncrementSensorRequestCount(string sensorId)
    {
        var today = DateTime.UtcNow.Date;

        // Reset the counter if the day has changed
        if (_currentDay != today)
        {
            _sensorRequestCounts.Clear();
        }

        _sensorRequestCounts.AddOrUpdate(sensorId, 1, (_, count) => count + 1);
    }
}