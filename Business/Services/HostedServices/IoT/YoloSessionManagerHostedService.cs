using Business.Services.TaskQueueServices;
using Microsoft.Extensions.Hosting;

namespace Business.Services.HostedServices.IoT;

public class YoloSessionManagerHostedService(IYoloSessionManager sessionManager) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var periodTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await periodTimer.WaitForNextTickAsync(stoppingToken))
        {
            sessionManager.CleanupExpiredSessions();
            await sessionManager.RunOneAsync(stoppingToken);
        }
    }
}