using Business.Business.Interfaces.FileSystem;
using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Business.Services.Ffmpeg;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Business.Utils.Helper;

namespace Business.Services.FileSystem;

public class FileSystemWatcherService(
    IOptions<AppSettings> appSettings,
    ILogger<FileSystemWatcherService> logger,
    ISequenceBackgroundTaskQueue taskQueueService,
    IParallelBackgroundTaskQueue parallelBackgroundTaskQueue,
    IFolderSystemBusinessLayer folderSystemBusinessLayer) : IHostedService
{
    private readonly List<FileSystemWatcher> _watchers = [];

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        taskQueueService.QueueBackgroundWorkItemAsync(async token =>
        {
            await e.FullPath.CheckFileSizeStable();

            var workDir = appSettings.Value.VideoTransCode.WorkingDirectory;

            await TerminalExtension.ExecuteCommandAsync($"./convert_to_hls.sh \"{e.FullPath}\"", workDir, token);

            var insertFilePath = Path.Combine(workDir, Path.GetFileName(e.FullPath));
            await folderSystemBusinessLayer.InsertMediaContent(insertFilePath, token);

            var outputFilePath = insertFilePath.Replace(Path.GetExtension(insertFilePath), "");
            logger.LogInformation($"Collecting garbage...{outputFilePath}");
            if (Directory.Exists(outputFilePath))
            {
                Directory.Delete(outputFilePath, true);
                logger.LogInformation($"Deleted {outputFilePath}");
            }
            else
            {
                logger.LogError($"Could not find {outputFilePath}");
            }
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var watchResources = appSettings.Value.Storage.FolderWatchList;
        var watchStorageResources = appSettings.Value.Storage.Disks;
        string[] watchExtensionFilters = ["*.mp4", "*.mkv"];

        foreach (var watchResource in watchResources)
        {
            if (!Directory.Exists(watchResource))
            {
                logger.LogError($"Could not find {watchResource}");
                continue;
            }

            foreach (var extension in watchExtensionFilters)
            {
                parallelBackgroundTaskQueue.QueueBackgroundWorkItemAsync(_ =>
                {
                    try
                    {
                        logger.LogInformation($"Watching files in {watchResource}. It may take a few minutes.");
                        FileSystemWatcher watcher = new FileSystemWatcher();
                        watcher.Path = watchResource;
                        watcher.Filter = extension;
                        watcher.Created += WatcherOnCreated;
                        watcher.EnableRaisingEvents = true;
                        watcher.IncludeSubdirectories = true;
                        _watchers.Add(watcher);
                        logger.LogInformation($"Watcher started. Now listen to file system changes on {watchResource} with extension {extension}.");
                        return ValueTask.CompletedTask;
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, e.Message);
                        return ValueTask.CompletedTask;
                    }
                }, cancellationToken);
            }
        }

        foreach (var storageResource in watchStorageResources)
        {
            if (!Directory.Exists(storageResource))
            {
                logger.LogError($"Could not find {storageResource}");
                continue;
            }

            parallelBackgroundTaskQueue.QueueBackgroundWorkItemAsync(_ =>
            {
                try
                {
                    logger.LogInformation($"Watching files in {storageResource}. It may take a few minutes.");
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = storageResource;
                    // watcher.Deleted += WatcherOnDeleted;
                    watcher.Error += WatcherOnError;
                    // watcher.Changed += WatchStorageResourcesChanged;
                    // watcher.Renamed += WatchStorageResourcesRenamed;
                    watcher.EnableRaisingEvents = true;
                    watcher.IncludeSubdirectories = true;
                    _watchers.Add(watcher);
                    logger.LogInformation($"Watcher started. Now listen to file system changes on {storageResource}.");
                    return ValueTask.CompletedTask;
                }
                catch (Exception e)
                {
                    logger.LogError(e, e.Message);
                    return ValueTask.CompletedTask;
                }
            }, cancellationToken);
        }

        return Task.CompletedTask;
    }


    private void WatcherOnError(object sender, ErrorEventArgs e)
    {
        logger.LogError($"{e}");
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _watchers.Clear();
        return Task.CompletedTask;
    }
}