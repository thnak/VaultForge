using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Business.Business.Interfaces.FileSystem;
using Business.Data;
using Business.Models;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.FileSystem;

public class FileCheckSumService(IFileSystemBusinessLayer fileSystemBusinessLayer, ILogger<FileCheckSumService> logger, RedundantArrayOfIndependentDisks disks, ISequenceBackgroundTaskQueue queue) : IHostedService
{
    private Timer? _timer;
    private bool _isRunning;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private const int BufferSize = 10 * 1024 * 1024;
    private const int ExpirationDays = 30;
    private const int FileStreamBuffer = 100 * 1024 * 1024;

    private void DoWork(object? state)
    {
        if (_isRunning) return;

        _isRunning = true;
        _ = Task.Run(async () =>
        {
            var cancelToken = _cancellationTokenSource.Token;
            var fieldToFetch = GetFieldsToFetch();
            var cursor = fileSystemBusinessLayer.Where(x => true, cancelToken, fieldToFetch);

            await foreach (var item in cursor)
            {
                await queue.QueueBackgroundWorkItemAsync(async (token) => await ProcessItemAsync(item, token), cancelToken);
            }

            _isRunning = false;
        });
    }

    private static Expression<Func<FileInfoModel, object>>[] GetFieldsToFetch()
    {
        return
        [
            model => model.Id,
            model => model.AbsolutePath,
            model => model.Checksum,
            model => model.ModifiedTime
        ];
    }

    private async Task ProcessItemAsync(FileInfoModel item, CancellationToken cancelToken)
    {
        if (disks.Exists(item.AbsolutePath))
        {
            await HandleExistingFileAsync(item, cancelToken);
        }
        else
        {
            await HandleMissingFileAsync(item, cancelToken);
        }
    }

    private async Task HandleExistingFileAsync(FileInfoModel item, CancellationToken cancelToken)
    {
        if (item.Status == FileStatus.DeletedFile)
        {
            var expirationTime = DateTime.UtcNow - item.ModifiedTime;
            if (expirationTime.TotalDays > ExpirationDays)
            {
                await fileSystemBusinessLayer.DeleteAsync(item.Id.ToString(), cancelToken);
                return;
            }
        }

        if (item.Status == FileStatus.CorruptedFile || item.Status == FileStatus.MissingFile)
        {
            logger.LogInformation($"File {item.AbsolutePath} is corrupted");
            return;
        }

        var (fileStream, buffer) = await ReadFileAsync(item, cancelToken);
        var checksum = CalculateChecksum(fileStream, buffer, cancelToken);
        await fileStream.DisposeAsync();

        await UpdateFileStatusAsync(item, checksum, cancelToken);
    }

    private async Task<(Stream, byte[])> ReadFileAsync(FileInfoModel item, CancellationToken cancelToken)
    {
        Stream fileStream = item.ContentType.IsImageFile() ? new MemoryStream() : new FileStream(Path.GetRandomFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, FileStreamBuffer, FileOptions.DeleteOnClose);

        await disks.ReadGetDataAsync(fileStream, item.AbsolutePath, cancelToken);
        var buffer = new byte[BufferSize];
        return (fileStream, buffer);
    }

    private string CalculateChecksum(Stream fileStream, byte[] buffer, CancellationToken cancelToken)
    {
        using MD5 md5 = MD5.Create();
        int readByte;
        while ((readByte = fileStream.ReadAsync(buffer, 0, BufferSize, cancelToken).Result) > 0)
        {
            md5.TransformBlock(buffer, 0, readByte, null, 0);
        }

        md5.TransformFinalBlock(new byte[0], 0, 0);

        StringBuilder checksum = new StringBuilder();
        if (md5.Hash != null)
        {
            foreach (byte b in md5.Hash)
            {
                checksum.Append(b.ToString("x2"));
            }
        }

        return checksum.ToString();
    }

    private async Task UpdateFileStatusAsync(FileInfoModel item, string checksum, CancellationToken cancelToken)
    {
        if (string.IsNullOrEmpty(item.Checksum))
        {
            await fileSystemBusinessLayer.UpdateAsync(item.Id.ToString(), new FieldUpdate<FileInfoModel>
            {
                { x => x.Checksum, checksum }
            }, cancelToken);
        }
        else if (item.Checksum != checksum)
        {
            logger.LogInformation($"File {item.AbsolutePath} is corrupted");
            await fileSystemBusinessLayer.UpdateAsync(item.Id.ToString(), new FieldUpdate<FileInfoModel>
            {
                { x => x.Status, FileStatus.CorruptedFile },
                { x => x.PreviousStatus, item.Status }
            }, cancelToken);
        }
    }

    private async Task HandleMissingFileAsync(FileInfoModel item, CancellationToken cancelToken)
    {
        logger.LogInformation($"File {item.AbsolutePath} does not exist");
        await fileSystemBusinessLayer.UpdateAsync(item.Id.ToString(), new FieldUpdate<FileInfoModel>
        {
            { x => x.Status, FileStatus.MissingFile },
            { x => x.PreviousStatus, item.Status }
        }, cancelToken);
    }

    private void Dispose()
    {
        _timer?.Dispose();
        _cancellationTokenSource.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var dueTime = DateTime.Today.AddDays(1).AddHours(2) - DateTime.Now;
        logger.LogInformation($"Starting FileCheckSumService after {dueTime}");
        _timer = new Timer(DoWork, null, dueTime, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }
}