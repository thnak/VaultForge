using Business.Data.Interfaces.User;

namespace CodeWithMe.Services;

public class StartupService(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = serviceScopeFactory.CreateScope())
        {
            scope.ServiceProvider.GetService<IUserDataLayer>()?.InitializeAsync();
        }

        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}