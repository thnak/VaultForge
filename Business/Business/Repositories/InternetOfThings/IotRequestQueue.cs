using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Business.Business.Interfaces.InternetOfThings;
using BusinessModels.General.SettingModels;
using BusinessModels.System.InternetOfThings;
using Microsoft.Extensions.Options;

namespace Business.Business.Repositories.InternetOfThings;

public class IotRequestQueue : IIotRequestQueue
{
    private readonly Channel<IoTRecord> _channel;

    public IotRequestQueue(IOptions<AppSettings> options)
    {
        var maxQueueSize = options.Value.IoTRequestQueueConfig.MaxQueueSize;
        BoundedChannelOptions boundedChannelOptions = new(maxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        };
        _channel = Channel.CreateBounded<IoTRecord>(boundedChannelOptions);
    }

    public async Task<bool> QueueRequest(IoTRecord data, CancellationToken cancellationToken = default)
    {
        return await _channel.Writer.WaitToWriteAsync(cancellationToken) && _channel.Writer.TryWrite(data);
    }

    public async ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return await _channel.Reader.WaitToReadAsync(cancellationToken);
    }

    public bool TryRead([MaybeNullWhen(false)] out IoTRecord item)
    {
        return _channel.Reader.TryRead(out item);
    }
}