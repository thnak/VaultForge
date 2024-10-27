using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Services.TaskQueueServices.Base;

public class ParallelBackgroundTaskQueue : IParallelBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public ParallelBackgroundTaskQueue(IOptions<AppSettings> appSettings, ILogger<ParallelBackgroundTaskQueue> logger)
    {
        var size = appSettings.Value.BackgroundQueue.ParallelQueueSize;
        logger.LogInformation($"Init parallel queue size is {size:N0}");
        BoundedChannelOptions options = new(size)
        {
            FullMode = BoundedChannelFullMode.Wait,
        };
        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
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