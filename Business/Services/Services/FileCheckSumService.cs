using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Business.Business.Interfaces.FileSystem;
using Business.Data;
using Business.Models;
using Business.Utils.Helper;
using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Business.Services.Services;

public class FileCheckSumService(IFileSystemBusinessLayer fileSystemBusinessLayer, ILogger<FileCheckSumService> logger, RedundantArrayOfIndependentDisks disks) : IHostedService
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
            var fieldToFetch = new Expression<Func<FileInfoModel, object>>[]
            {
                model => model.Id,
                model => model.AbsolutePath,
                model => model.Checksum,
                model => model.ModifiedTime,
            };
            var cursor = fileSystemBusinessLayer.Where(x => true, cancelToken, fieldToFetch);
            await foreach (var item in cursor)
            {
                if (disks.Exists(item.AbsolutePath))
                {
                    if (item.Type == FileContentType.DeletedFile)
                    {
                        var expirationTime = DateTime.UtcNow - item.ModifiedTime;
                        if (expirationTime.TotalDays > 30)
                        {
                            await fileSystemBusinessLayer.DeleteAsync(item.Id.ToString(), cancelToken);
                            continue;
                        }
                    }

                    Stream fileStream;
                    if (item.ContentType.IsImageFile())
                        fileStream = new MemoryStream();
                    else
                    {
                        fileStream = new FileStream(Path.GetRandomFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, bufferSize: 100 * 1024 * 1024, FileOptions.DeleteOnClose);
                    }

                    await disks.ReadGetDataAsync(fileStream, item.AbsolutePath, cancelToken);
                    
                    int readByte;
                    var buffer = new byte[BufferSize];
                    using MD5 sha256 = MD5.Create();
                    while ((readByte = await fileStream.ReadAsync(buffer, 0, BufferSize, cancelToken)) > 0)
                    {
                        sha256.TransformBlock(buffer, 0, readByte, null, 0);
                    }

                    await fileStream.DisposeAsync();
                    
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
                                { x => x.Type, FileContentType.CorruptedFile },
                                { x=> x.PreviousType, item.Type }
                            }, cancelToken);
                        }
                    }
                }
                else
                {
                    logger.LogInformation($"File {item.AbsolutePath} does not exist");
                    await fileSystemBusinessLayer.UpdateAsync(item.Id.ToString(), new FieldUpdate<FileInfoModel>()
                    {
                        { x => x.Type, FileContentType.MissingFile },
                        { x=> x.PreviousType, item.Type }
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