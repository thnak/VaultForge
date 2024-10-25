using Business.Business.Interfaces.Advertisement;
using Business.Business.Interfaces.Chat;
using Business.Business.Interfaces.FileSystem;
using Business.Business.Interfaces.InternetOfThings;
using Business.Business.Interfaces.User;
using Business.Business.Repositories.Advertisement;
using Business.Business.Repositories.Chat;
using Business.Business.Repositories.FileSystem;
using Business.Business.Repositories.InternetOfThings;
using Business.Business.Repositories.User;
using Business.Data;
using Business.Data.Interfaces;
using Business.Data.Interfaces.Advertisement;
using Business.Data.Interfaces.Chat;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.InternetOfThings;
using Business.Data.Interfaces.User;
using Business.Data.Repositories;
using Business.Data.Repositories.Advertisement;
using Business.Data.Repositories.Chat;
using Business.Data.Repositories.FileSystem;
using Business.Data.Repositories.InternetOfThings;
using Business.Data.Repositories.User;
using Business.Services.FileSystem;
using Business.Services.Interfaces;
using Business.Services.TaskQueueServices;
using Business.Services.TaskQueueServices.Base;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.Configure;

public static class DataService
{
    public static IServiceCollection AddDataServiceCollection(this IServiceCollection service)
    {
        service.AddSingleton<IMongoDataLayerContext, MongoDataLayerContext>();

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

        service.AddSingleton<IThumbnailService, ThumbnailService>();

        service.AddSingleton<IIoTDataLayer, IoTDataLayer>();
        service.AddSingleton<IIoTBusinessLayer, IoTBusinessLayer>();

        service.AddHostedService<FileSystemWatcherService>();

        service.AddSingleton<IParallelBackgroundTaskQueue, ParallelBackgroundTaskQueue>();
        service.AddSingleton<ISequenceBackgroundTaskQueue, SequenceBackgroundTaskQueue>();

        service.AddHostedService<HostApplicationLifetimeEventsHostedService>();
        service.AddHostedService<FileCheckSumService>();

        service.AddHostedService<SequenceQueuedHostedService>();
        service.AddHostedService<ParallelQueuedHostedService>();

        return service;
    }

}