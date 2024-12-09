using Business.Business.Interfaces.Advertisement;
using Business.Business.Interfaces.Chat;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.InternetOfThings;
using Business.Business.Interfaces.User;
using Business.Business.Interfaces.Wiki;
using Business.Business.Repositories.Advertisement;
using Business.Business.Repositories.Chat;
using Business.Business.Repositories.FileSystem;
using Business.Business.Repositories.InternetOfThings;
using Business.Business.Repositories.User;
using Business.Business.Repositories.Wiki;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Advertisement;
using Business.Data.Interfaces.Chat;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.InternetOfThings;
using Business.Data.Interfaces.User;
using Business.Data.Interfaces.VectorDb;
using Business.Data.Interfaces.Wiki;
using Business.Data.Repositories;
using Business.Data.Repositories.Advertisement;
using Business.Data.Repositories.Chat;
using Business.Data.Repositories.FileSystem;
using Business.Data.Repositories.InternetOfThings;
using Business.Data.Repositories.User;
using Business.Data.Repositories.VectorDb;
using Business.Data.Repositories.Wiki;
using Business.Data.StorageSpace;
using Business.Services.BackgroundServices.Base;
using Business.Services.BackgroundServices.ServerHealth;
using Business.Services.FileSystem;
using Business.Services.HostedServices.Base;
using Business.Services.HostedServices.FileSystem;
using Business.Services.Interfaces;
using Business.Services.TaskQueueServices.Base;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.SettingModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Business.Services.Configure;

public static class DataService
{
    public static void AddDataServiceCollection(this IServiceCollection service)
    {
        service.AddAdvancedSystemServices();

        service.AddSingleton<IMongoDataLayerContext, MongoDataLayerContext>();

        service.AddSingleton<IVectorDataLayer, VectorDataLayer>();
        
        service.AddSingleton<RedundantArrayOfIndependentDisks>();

        service.AddSingleton<IUserDataLayer, UserDataLayer>();
        service.AddSingleton<IUserBusinessLayer, UserBusinessLayer>();

        service.AddSingleton<IFolderSystemDatalayer, FolderSystemDatalayer>();
        service.AddSingleton<IFolderSystemBusinessLayer, FolderSystemBusinessLayer>();

        service.AddSingleton<IFileSystemDatalayer, FileSystemDatalayer>();
        service.AddSingleton<IFileSystemBusinessLayer, FileSystemBusinessLayer>();

        service.AddSingleton<IAdvertisementDataLayer, AdvertisementDataLayer>();
        service.AddSingleton<IAdvertisementBusinessLayer, AdvertisementBusinessLayer>();

        service.AddSingleton<IChatWithLlmDataLayer, ChatWithLlmDataLayer>();
        service.AddSingleton<IChatWithLlmBusinessLayer, ChatWithLlmBusinessLayer>();

        service.AddSingleton<IIoTDataLayer, IoTDataLayer>();
        service.AddSingleton<IIoTBusinessLayer, IoTBusinessLayer>();

        service.AddSingleton<IFaceDataLayer, FaceDataLayer>();
        service.AddSingleton<IFaceBusinessLayer, FaceBusinessLayer>();

        service.AddSingleton<IWikipediaDataLayer, WikipediaDataLayer>();
        service.AddSingleton<IWikipediaBusinessLayer, WikipediaBusinessLayer>();

        service.AddHostedService<HostApplicationLifetimeEventsHostedService>();

        service.AddAdvancedServiceCollection();


        service.Configure<HostOptions>(options =>
        {
            options.ServicesStartConcurrently = false;
            options.ServicesStopConcurrently = false;
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
        });
    }

    public static void AddAppOptions(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        builder.Services.AddSingleton<ApplicationConfiguration>();
    }

    private static void AddAdvancedSystemServices(this IServiceCollection service)
    {
        service.AddSingleton<IParallelBackgroundTaskQueue, ParallelBackgroundTaskQueue>();
        service.AddSingleton<ISequenceBackgroundTaskQueue, SequenceBackgroundTaskQueue>();
        service.AddHostedService<Worker>();
        service.AddHostedService<SequenceQueuedBackgroundService>();
        service.AddHostedService<ParallelBackgroundService>();
    }

    private static void AddAdvancedServiceCollection(this IServiceCollection service)
    {
        service.AddSingleton<IThumbnailService, ThumbnailService>();
        service.AddHostedService<FileCheckSumHostedService>();
        service.AddHostedService<FileSystemWatcherHostedService>();
    }
}