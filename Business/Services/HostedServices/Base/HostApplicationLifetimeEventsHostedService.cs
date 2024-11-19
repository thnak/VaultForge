using Business.Business.Interfaces;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.User;
using Business.Business.Interfaces.Wiki;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Advertisement;
using Business.Data.Interfaces.Chat;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.User;
using Business.Data.Interfaces.Wiki;
using Business.Data.StorageSpace;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.HostedServices.Base;

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
        logger.LogInformation("Application started");
        QueueInitializationTask<IUserDataLayer>();
        QueueInitializationTask<IFileSystemDatalayer>();
        QueueInitializationTask<IFolderSystemDatalayer>();
        QueueInitializationTask<IAdvertisementDataLayer>();
        QueueInitializationTask<IChatWithLlmDataLayer>();
        QueueInitializationTask<IFaceDataLayer>();
        QueueInitializationTask<IWikipediaDataLayer>();


        QueueInitializationTask<RedundantArrayOfIndependentDisks>();
        QueueInitializationExtendServiceTask<IFolderSystemBusinessLayer>();
        QueueInitializationExtendServiceTask<IFaceBusinessLayer>();
        QueueInitializationExtendServiceTask<IWikipediaBusinessLayer>();
    }

    private void QueueInitializationExtendServiceTask<TDataLayer>() where TDataLayer : IExtendService
    {
        queue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dataLayer = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            var result = await dataLayer.InitializeAsync(token);
            if (result.IsSuccess)
                logger.LogInformation(result.Message);
            else
            {
                logger.LogError(result.Message);
            }
        });
    }

    private void QueueInitializationTask<TDataLayer>() where TDataLayer : IMongoDataInitializer
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