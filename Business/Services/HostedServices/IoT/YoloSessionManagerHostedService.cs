using Business.Services.TaskQueueServices;
using Microsoft.Extensions.Hosting;

namespace Business.Services.HostedServices.IoT;

public class YoloSessionManagerHostedService(IYoloSessionManager sessionManager, TimeProvider timeProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var periodTimer = new PeriodicTimer(TimeSpan.FromSeconds(1), timeProvider);
        while (await periodTimer.WaitForNextTickAsync(stoppingToken))
        {
            sessionManager.CleanupExpiredSessions();
            await sessionManager.RunOneAsync(stoppingToken);
        }
    }
}