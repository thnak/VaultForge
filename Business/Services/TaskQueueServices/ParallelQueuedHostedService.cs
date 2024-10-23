using Business.Services.TaskQueueServices.Base;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Business.Services.TaskQueueServices;

public class ParallelQueuedHostedService : BackgroundService
{
    private DefaultBackgroundTaskQueue TaskQueue { get; set; }
    private LimitedConcurrencyLevelTaskScheduler lcts;
    private readonly ILogger<ParallelQueuedHostedService> logger;
    private readonly TaskFactory factory;

    public ParallelQueuedHostedService(IOptions<AppSettings> options, ILogger<ParallelQueuedHostedService> logger)
    {
        TaskQueue = new DefaultBackgroundTaskQueue(options);
        lcts = new LimitedConcurrencyLevelTaskScheduler(Environment.ProcessorCount - 2);
        this.logger = logger;
        factory = new TaskFactory(lcts);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                List<Task> tasks = new List<Task>();

                while (TaskQueue.Count > 0)
                {
                    Func<CancellationToken, ValueTask> workItem = await TaskQueue.DequeueAsync(stoppingToken);
                    var task = factory.StartNew(() => workItem(stoppingToken), stoppingToken);
                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray(), stoppingToken);
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