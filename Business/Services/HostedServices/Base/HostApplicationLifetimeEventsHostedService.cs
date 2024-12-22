using Business.Business.Interfaces;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.InternetOfThings;
using Business.Business.Interfaces.User;
using Business.Business.Interfaces.Wiki;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Advertisement;
using Business.Data.Interfaces.Chat;
using Business.Data.Interfaces.ComputeVision;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.User;
using Business.Data.Interfaces.VectorDb;
using Business.Data.Interfaces.Wiki;
using Business.Data.StorageSpace;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.HostedServices.Base;

public class HostApplicationLifetimeEventsHostedService(
    IHostApplicationLifetime hostApplicationLifetime,
    IParallelBackgroundTaskQueue queue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<HostApplicationLifetimeEventsHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await OnStarted(cancellationToken);
        hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
        hostApplicationLifetime.ApplicationStopped.Register(OnStopped);
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task OnStarted(CancellationToken cancellationToken)
    {
        logger.LogInformation("Application started");
        await QueueInitializationTask<IVectorDataLayer>(cancellationToken);
        await QueueInitializationTask<IUserDataLayer>(cancellationToken);
        await QueueInitializationTask<IFileSystemDatalayer>(cancellationToken);
        await QueueInitializationTask<IFolderSystemDatalayer>(cancellationToken);
        await QueueInitializationTask<IAdvertisementDataLayer>(cancellationToken);
        await QueueInitializationTask<IChatWithLlmDataLayer>(cancellationToken);
        await QueueInitializationTask<IFaceDataLayer>(cancellationToken);
        await QueueInitializationTask<IWikipediaDataLayer>(cancellationToken);
        await QueueInitializationTask<RedundantArrayOfIndependentDisks>(cancellationToken);
        await QueueInitializationTask<IYoloLabelDataLayer>(cancellationToken);


        await QueueInitializationExtendServiceTask<IFolderSystemBusinessLayer>(cancellationToken);
        await QueueInitializationExtendServiceTask<IFaceBusinessLayer>(cancellationToken);
        await QueueInitializationExtendServiceTask<IWikipediaBusinessLayer>(cancellationToken);
        await QueueInitializationExtendServiceTask<IIotRecordBusinessLayer>(cancellationToken);
    }

    private async Task QueueInitializationExtendServiceTask<TDataLayer>(CancellationToken cancellationToken) where TDataLayer : IExtendService
    {
        await queue.QueueBackgroundWorkItemAsync(async token =>
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
        }, cancellationToken);
    }

    private async Task QueueInitializationTask<TDataLayer>(CancellationToken cancellationToken) where TDataLayer : IMongoDataInitializer
    {
        await queue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dataLayer = scope.ServiceProvider.GetRequiredService<TDataLayer>();
            await dataLayer.InitializeAsync(token);
        }, cancellationToken);
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