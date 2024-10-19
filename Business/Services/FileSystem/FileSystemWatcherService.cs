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

namespace Business.Services.FileSystem;

public class FileSystemWatcherService(
    IOptions<AppSettings> appSettings,
    ILogger<FileSystemWatcherService> logger,
    IBackgroundTaskQueue taskQueueService,
    IFolderSystemBusinessLayer folderSystemBusinessLayer,
    IFileSystemBusinessLayer fileSystemBusinessLayer,
    RedundantArrayOfIndependentDisks raidService) : IHostedService
{
    private readonly List<FileSystemWatcher> _watchers = [];

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        taskQueueService.QueueBackgroundWorkItemAsync(async token =>
        {
            var extension = Path.GetExtension(e.FullPath);
            string[] allowedExtensions = [".mp4", ".mkv"];
            if (!allowedExtensions.Contains(extension))
            {
#if DEBUG
                logger.LogWarning($"File {e.FullPath} is in an invalid format");
#endif
                return;
            }

            var terminalResult = TerminalExtension.ExecuteCommand($" ./convert_to_hls.sh {e.FullPath}");
            logger.LogInformation($"Terminal result: {terminalResult}");
            var outputDir = Path.GetFileNameWithoutExtension(e.FullPath);
            outputDir = Path.Combine(Directory.GetCurrentDirectory(), outputDir);
            
            if (!Directory.Exists(outputDir))
            {
                logger.LogInformation($"Output directory doesn't exist: {outputDir}");
                return;
            }

            var m3U8Files = Directory.GetFiles(outputDir, "playlist.m3u8", SearchOption.AllDirectories).ToArray();

            var rootVideoFolder = folderSystemBusinessLayer.Get("Anonymous", "/root/Videos");

            if (rootVideoFolder == null)
            {
                logger.LogError($"Root video folder was not found. Skipping {e.FullPath}.");
                return;
            }

            var storageFolder = new FolderInfoModel()
            {
                FolderName = Path.GetFileName(e.FullPath),
                Type = FolderContentType.SystemFolder,
                RootFolder = rootVideoFolder.Id.ToString()
            };

            var createFolderStorageResult = await folderSystemBusinessLayer.CreateFolder(new RequestNewFolderModel()
            {
                NewFolder = storageFolder,
                RootId = rootVideoFolder.Id.ToString(),
            });

            if (!createFolderStorageResult.Item1)
            {
                logger.LogError($"Failed to create folder: {createFolderStorageResult.Item2}");
                return;
            }

            foreach (var file in m3U8Files)
            {
                await ReadM3U8Files(storageFolder, file, token);
            }
            Directory.Delete(outputDir, true);
        });
    }

    private async Task ReadM3U8Files(FolderInfoModel folderStorage, string path, CancellationToken cancellationToken = default)
    {
        var playListContents = await File.ReadAllLinesAsync(path, cancellationToken);
        string m3U8FileId = string.Empty;

        for (int i = 0; i < playListContents.Length; i++)
        {
            var lineText = playListContents[i];
            if (lineText.EndsWith(".m3u8"))
            {
                var fileInfo = new FileInfoModel()
                {
                    FileName = lineText,
                    ContentType = "application/x-mpegURL",
                    Type = FileContentType.M3U8File
                };
                await folderSystemBusinessLayer.CreateFileAsync(folderStorage, fileInfo, cancellationToken);
                await ReadM3U8Files(folderStorage, lineText);
                m3U8FileId = fileInfo.Id.ToString();
                playListContents[i] = m3U8FileId + ".m3u8";
            }

            if (lineText.EndsWith(".ts") || lineText.EndsWith(".vtt"))
            {
                var fileInfo = new FileInfoModel()
                {
                    FileName = lineText,
                    ContentType = lineText.EndsWith(".ts") ? "video/MP2T" : "text/vtt",
                    Type = FileContentType.M3U8FileSegment
                };
                await folderSystemBusinessLayer.CreateFileAsync(folderStorage, fileInfo, cancellationToken);
                playListContents[i] = fileInfo.Id.ToString();
                await using var stream = new FileStream(lineText, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);
                var writeResult = await raidService.WriteDataAsync(stream, fileInfo.AbsolutePath, cancellationToken);

                await fileSystemBusinessLayer.UpdateAsync(playListContents[i], new FieldUpdate<FileInfoModel>()
                {
                    { x => x.Checksum, writeResult.CheckSum },
                    { x => x.ContentType, writeResult.ContentType },
                    { x => x.Checksum, writeResult.CheckSum }
                }, cancellationToken);
                playListContents[i] += lineText.EndsWith(".ts") ? ".ts" : ".vtt";
            }
        }

        if (!string.IsNullOrEmpty(m3U8FileId))
        {
            await using MemoryStream ms = new MemoryStream();
            await using StreamWriter sw = new StreamWriter(ms);
            foreach (var line in playListContents)
            {
                await sw.WriteLineAsync(line);
            }

            ms.Seek(0, SeekOrigin.Begin);

            var m3U8 = fileSystemBusinessLayer.Get(m3U8FileId);
            if (m3U8 == null)
            {
                logger.LogInformation($"M3U8: {m3U8FileId} ERROR");
                return;
            }

            var writeResult = await raidService.WriteDataAsync(ms, m3U8.AbsolutePath, cancellationToken);

            await fileSystemBusinessLayer.UpdateAsync(m3U8FileId, new FieldUpdate<FileInfoModel>()
            {
                { x => x.Checksum, writeResult.CheckSum },
                { x => x.ContentType, writeResult.ContentType },
                { x => x.Checksum, writeResult.CheckSum }
            }, cancellationToken);
        }
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