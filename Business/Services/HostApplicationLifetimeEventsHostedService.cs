using Business.Data.Interfaces.Advertisement;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Business.Services;

public class HostApplicationLifetimeEventsHostedService(IHostApplicationLifetime hostApplicationLifetime, IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
        hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnStarted()
    {
        using var scope = serviceScopeFactory.CreateScope();
        scope.ServiceProvider.GetService<IUserDataLayer>()!.InitializeAsync();
        scope.ServiceProvider.GetService<IFileSystemDatalayer>()?.InitializeAsync();
        scope.ServiceProvider.GetService<IFolderSystemDatalayer>()?.InitializeAsync();
        scope.ServiceProvider.GetService<IAdvertisementDataLayer>()?.InitializeAsync();
    }

    private void OnStopping()
    {
        Console.WriteLine(@"OnStopping");
        // ...
    }

    private void OnStopped()
    {
        Console.WriteLine(@"OnStopped");
    }
}