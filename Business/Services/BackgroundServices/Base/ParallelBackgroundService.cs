using Business.Services.Configure;
using Business.Services.TaskQueueServices.Base;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.BackgroundServices.Base;

public class ParallelBackgroundService(IParallelBackgroundTaskQueue parallelBackgroundTaskQueue, ApplicationConfiguration options, ILogger<ParallelBackgroundService> logger) : BackgroundService
{
    private readonly TaskFactory _factory = new(new LimitedConcurrencyLevelTaskScheduler(options.GetBackgroundQueue.MaxParallelThreads));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("""{Name} is running with max {threads} threads.""", nameof(ParallelBackgroundService), options.GetBackgroundQueue.MaxParallelThreads);
        return ProcessTaskQueueAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(ParallelBackgroundService)} is stopping.");
        await base.StopAsync(stoppingToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                while (parallelBackgroundTaskQueue.TryDequeue(out Func<CancellationToken, ValueTask>? workItem))
                {
                    await _factory.StartNew(async () => await workItem(stoppingToken), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation($"{nameof(ParallelBackgroundService)} cancelled.");
        }
    }
}