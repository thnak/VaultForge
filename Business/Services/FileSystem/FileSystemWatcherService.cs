using Business.Business.Interfaces.FileSystem;
using Business.Data;
using Business.Models;
using Business.Services.TaskQueueServices.Base;
using BusinessModels.General.EnumModel;
using BusinessModels.General.SettingModels;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Business.Services.Ffmpeg;
using Business.Utils.Helper;

namespace Business.Services.FileSystem;

public class FileSystemWatcherService(
    IOptions<AppSettings> appSettings,
    ILogger<FileSystemWatcherService> logger,
    IBackgroundTaskQueue taskQueueService,
    IFolderSystemBusinessLayer folderSystemBusinessLayer) : IHostedService
{
    private readonly List<FileSystemWatcher> _watchers = [];

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        taskQueueService.QueueBackgroundWorkItemAsync(async token =>
        {
            await e.FullPath.CheckFileSizeStable();

            var workDir = appSettings.Value.TransCodeConverterScriptDir;

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
        var watchResources = appSettings.Value.FolderWatchList;
        var watchStorageResources = appSettings.Value.FileFolders;
        string[] watchExtensionFilters = ["*.mp4", "*.mkv"];

        foreach (var watchResource in watchResources)
        {
            foreach (var extension in watchExtensionFilters)
            {
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = watchResource;
                watcher.Filter = extension;
                watcher.Created += WatcherOnCreated;
                watcher.EnableRaisingEvents = true;
                watcher.IncludeSubdirectories = true;
                _watchers.Add(watcher);
                logger.LogInformation($"Watcher started. Now listen to file system changes on {watchResource} with extension {extension}.");
            }
        }

        foreach (var storageResource in watchStorageResources)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = storageResource;
            watcher.Deleted += WatcherOnDeleted;
            watcher.Error += WatcherOnError;
            watcher.Changed += WatchStorageResourcesChanged;
            watcher.Renamed += WatchStorageResourcesRenamed;
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;
            _watchers.Add(watcher);
            logger.LogInformation($"Watcher started. Now listen to file system changes on {storageResource}.");
        }

        return Task.CompletedTask;
    }

    private void WatchStorageResourcesRenamed(object sender, RenamedEventArgs e)
    {
        logger.LogWarning($"{e.OldFullPath} was renamed to {e.FullPath}");
    }

    private void WatchStorageResourcesChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Created && e.ChangeType != WatcherChangeTypes.Deleted)
            logger.LogWarning($"{e.FullPath} watch storage resources changed [{e.ChangeType}].");
    }

    private void WatcherOnError(object sender, ErrorEventArgs e)
    {
        logger.LogError($"{e}");
    }

    private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        logger.LogWarning($"{e.FullPath} deleted");
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