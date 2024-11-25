using Business.Services.TaskQueueServices.Base.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.BackgroundServices.Base;

public sealed class SequenceQueuedBackgroundService(ISequenceBackgroundTaskQueue taskQueue, ILogger<SequenceQueuedBackgroundService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("""{Name} is running.""", nameof(SequenceQueuedBackgroundService));
        return ProcessTaskQueueAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                while (taskQueue.TryDequeue(out Func<CancellationToken, ValueTask>? workItem))
                {
                    await workItem(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("""{Name} is cancelled""", nameof(SequenceQueuedBackgroundService));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(SequenceQueuedBackgroundService)} is stopping.");
        await base.StopAsync(stoppingToken);
    }
}