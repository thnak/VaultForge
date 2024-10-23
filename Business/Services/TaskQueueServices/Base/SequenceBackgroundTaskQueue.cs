using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Options;

namespace Business.Services.TaskQueueServices.Base;

public sealed class SequenceBackgroundTaskQueue : ISequenceBackgroundTaskQueue
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

    public SequenceBackgroundTaskQueue(IOptions<AppSettings> appSettings)
    {
        var size = appSettings.Value.BackgroundQueue.SequenceQueueSize;
        BoundedChannelOptions options = new(size)
        {
            FullMode = BoundedChannelFullMode.Wait,
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        try
        {
            await _queue.Writer.WriteAsync(workItem, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            //
        }
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, ValueTask> workItem = await _queue.Reader.ReadAsync(cancellationToken);
        return workItem;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out Func<CancellationToken, ValueTask> workItem)
    {
        return _queue.Reader.TryRead(out workItem);
    }
}