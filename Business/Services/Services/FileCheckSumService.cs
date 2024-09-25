using System.Security.Cryptography;
using System.Text;
using Business.Business.Interfaces.FileSystem;
using Business.Models;
using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.Services;

public class FileCheckSumService(IFileSystemBusinessLayer fileSystemBusinessLayer, ILogger<FileCheckSumService> logger) : IHostedService
{
    private Timer? _timer;
    private bool _isRunning;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private const int BufferSize = 10 * 1024 * 1024;

    private void DoWork(object? state)
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _ = Task.Run(async () =>
        {
            var cancelToken = _cancellationTokenSource.Token;
            var cursor = fileSystemBusinessLayer.Where(x => true, cancelToken, model => model.Id, model => model.AbsolutePath, model => model.Checksum);
            await foreach (var item in cursor)
            {
                if (File.Exists(item.AbsolutePath))
                {
                    await using FileStream fileStream = new FileStream(item.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
                    int readByte;
                    var buffer = new byte[BufferSize];
                    using SHA256 sha256 = SHA256.Create();
                    while ((readByte = await fileStream.ReadAsync(buffer, 0, BufferSize, cancelToken)) > 0)
                    {
                        sha256.TransformBlock(buffer, 0, readByte, null, 0);
                    }

                    sha256.TransformFinalBlock([], 0, 0);
                    StringBuilder checksum = new StringBuilder();
                    if (sha256.Hash != null)
                    {
                        foreach (byte b in sha256.Hash)
                        {
                            checksum.Append(b.ToString("x2"));
                        }
                    }

                    var checkSumStr = checksum.ToString();
                    if (string.IsNullOrEmpty(item.Checksum))
                    {
                        await fileSystemBusinessLayer.UpdateAsync(item.Id.ToString(), new FieldUpdate<FileInfoModel>()
                        {
                            { x => x.Checksum, checkSumStr }
                        }, cancelToken);
                    }
                    else
                    {
                        if (item.Checksum != checkSumStr)
                        {
                            logger.LogInformation($"File {item.AbsolutePath} is corrupted");
                            await fileSystemBusinessLayer.UpdateAsync(item.Id.ToString(), new FieldUpdate<FileInfoModel>()
                            {
                                { x => x.Type, FileContentType.CorruptedFile }
                            }, cancelToken);
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"File {item.AbsolutePath} does not exist");
                    await fileSystemBusinessLayer.UpdateAsync(item.Id.ToString(), new FieldUpdate<FileInfoModel>()
                    {
                        { x => x.Type, FileContentType.MissingFile }
                    }, cancelToken);
                }
            }

            _isRunning = false;
        });
    }

    public void Dispose()
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
        _timer?.Dispose();
        _cancellationTokenSource.Dispose();
        return Task.CompletedTask;
    }
}