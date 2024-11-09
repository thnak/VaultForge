using System.Diagnostics.CodeAnalysis;

namespace Business.Services.TaskQueueServices.Base.Interfaces;

public interface IBackgroundTaskQueue
{
    public int CountItemSize();
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem, CancellationToken cancellationToken = default);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
    bool TryDequeue([MaybeNullWhen(false)] out Func<CancellationToken, ValueTask> workItem);
}