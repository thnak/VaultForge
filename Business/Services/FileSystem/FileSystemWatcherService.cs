using BusinessModels.General.SettingModels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
#if DEBUG
using Business.Services.Ffmpeg;
#endif

namespace Business.Services.FileSystem;

public class FileSystemWatcherService(IOptions<AppSettings> appSettings, ILogger<FileSystemWatcherService> logger) : IHostedService
{
    private readonly List<FileSystemWatcher> _watchers = [];

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
#if DEBUG
        FFmpegService.EncodeVideo(e.FullPath, Path.GetDirectoryName(e.FullPath) ?? throw new InvalidOperationException());
#else
#endif
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var watchResources = appSettings.Value.FolderWatchList;
        foreach (var watchResource in watchResources)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = watchResource;
            watcher.Filter = "";
            watcher.Created += WatcherOnCreated;
            watcher.EnableRaisingEvents = true;
            _watchers.Add(watcher);
            logger.LogInformation($"Watcher started. Now listen to file system changes on {watchResource}.");
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