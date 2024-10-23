using System.Threading.Channels;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Options;

namespace Business.Services.TaskQueueServices.Base;

public sealed class DefaultBackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public int Count
    {
        get
        {
            if (_queue.Reader.CanCount)
                return _queue.Reader.Count;
            return 0;
        }
    }

    public DefaultBackgroundTaskQueue(IOptions<AppSettings> appSettings)
    {
        var size = appSettings.Value.BackgroundStackQueueSize;
        BoundedChannelOptions options = new(size)
        {
            FullMode = BoundedChannelFullMode.Wait,
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, ValueTask> workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }
}