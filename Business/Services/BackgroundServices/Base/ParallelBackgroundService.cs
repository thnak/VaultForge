using Business.Services.TaskQueueServices.Base;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Services.BackgroundServices.Base;

public class ParallelBackgroundService(IParallelBackgroundTaskQueue parallelBackgroundTaskQueue, IOptions<AppSettings> options, ILogger<ParallelBackgroundService> logger) : BackgroundService
{
    private readonly TaskFactory _factory = new(new LimitedConcurrencyLevelTaskScheduler(options.Value.BackgroundQueue.MaxParallelThreads));

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("""{Name} is running with max {threads} threads.""", nameof(ParallelBackgroundService), options.Value.BackgroundQueue.MaxParallelThreads);
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
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            while (parallelBackgroundTaskQueue.TryDequeue(out Func<CancellationToken, ValueTask>? workItem))
            {
                _ = _factory.StartNew(async () => await workItem(stoppingToken), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}