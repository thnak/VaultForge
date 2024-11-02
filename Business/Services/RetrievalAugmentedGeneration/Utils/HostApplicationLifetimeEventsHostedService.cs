using Business.Services.RetrievalAugmentedGeneration.Interface;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.RetrievalAugmentedGeneration.Utils;

public class HostApplicationLifetimeEventsHostedService(IHostApplicationLifetime hostApplicationLifetime, IParallelBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory, ILogger<HostApplicationLifetimeEventsHostedService> logger) : IHostedService
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
        logger.LogInformation("Adding RAG services");
        QueueInitializationTask<IMovieDatabase>();
    }

    private void QueueInitializationTask<TDataLayer>() where TDataLayer : IBaseInitialize
    {
        queue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dataLayer = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            await dataLayer.InitializeAsync(token);
        });
    }

    private void OnStopping()
    {
        logger.LogInformation("Application stopping");
    }

    private void OnStopped()
    {
        logger.LogInformation("Application stopped");
    }
}