using Business.SignalRHub.System.Implement;
using Business.SignalRHub.System.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace Business.Services.BackgroundServices.ServerHealth;

public class Worker(IHubContext<ClockHub, IClock> clockHub) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await clockHub.Clients.All.ShowTime(DateTime.UtcNow);
            await Task.Delay(1000, stoppingToken);
        }
    }
}