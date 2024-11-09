using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Services.TaskQueueServices.Base;

public sealed class SequenceBackgroundTaskQueue : ISequenceBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public SequenceBackgroundTaskQueue(IOptions<AppSettings> appSettings, ILogger<SequenceBackgroundTaskQueue> logger)
    {
        var size = appSettings.Value.BackgroundQueue.SequenceQueueSize;
        logger.LogInformation($"Init sequence queue size is {size:N0}");
        BoundedChannelOptions options = new(size)
        {
            FullMode = BoundedChannelFullMode.Wait,
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public int CountItemSize()
    {
        if (_queue.Reader.CanCount)
            return _queue.Reader.Count;
        return 0;
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default)
    {
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