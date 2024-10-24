using Business.Services.TaskQueueServices.Base;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Services.TaskQueueServices;

public class ParallelQueuedHostedService(IParallelBackgroundTaskQueue parallelBackgroundTaskQueue, IOptions<AppSettings> options, ILogger<ParallelQueuedHostedService> logger) : BackgroundService
{
    private readonly TaskFactory _factory = new(new LimitedConcurrencyLevelTaskScheduler(options.Value.BackgroundQueue.MaxParallelThreads));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Task> tasks = new List<Task>();

                while (parallelBackgroundTaskQueue.TryDequeue(out Func<CancellationToken, ValueTask>? workItem))
                {
                    var task = _factory.StartNew(async () => await workItem(stoppingToken), stoppingToken);
                    tasks.Add(task);
                }

                await Task.WhenAll(tasks); 
                await Task.Delay(500, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing task work item.");
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"{nameof(ParallelQueuedHostedService)} is stopping.");
        await base.StopAsync(stoppingToken);
    }
}