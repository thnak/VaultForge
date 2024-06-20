namespace Web.Services;

public class HostApplicationLifetimeEventsHostedService(IHostApplicationLifetime hostApplicationLifetime) : IHostedService
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
        Console.WriteLine(@"OnStarted");
        // ...
    }

    private void OnStopping()
    {
        Console.WriteLine(@"OnStopping");
        Thread.Sleep(100_000);
        // ...
    }

    private void OnStopped()
    {
        Console.WriteLine(@"OnStopped");
    }
}