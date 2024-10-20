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
            if (Directory.Exists(outputFilePath)) Directory.Delete(outputFilePath, true);
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var watchResources = appSettings.Value.FolderWatchList;
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

        return Task.CompletedTask;
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