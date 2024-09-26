using System.Collections.Concurrent;
using Business.Business.Interfaces.FileSystem;
using Business.Models;
using Business.Services.Interfaces;
using Business.Utils.Helper;
using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Business.Services.Services;

public class ThumbnailService(IServiceProvider serviceProvider, ILogger<ThumbnailService> logger) : IThumbnailService, IDisposable
{
    private readonly BlockingCollection<string> _thumbnailQueue = new();
    private SemaphoreSlim QueueSemaphore { get; } = new(Environment.ProcessorCount - 1);
    private const int MaxDimension = 480; // Maximum width or height
    private const int BufferSize = 10 * 1024 * 1024;
    private ILogger<ThumbnailService> Logger { get; } = logger;
    private IServiceProvider ServiceProvider { get; } = serviceProvider;

    // To resolve database and image services
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Task AddThumbnailRequest(string imageId)
    {
        _thumbnailQueue.Add(imageId);
        return Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Thumbnail Service started.");

        while (_thumbnailQueue.TryTake(out string? imageId, -1, cancellationToken))
        {
            if (string.IsNullOrEmpty(imageId)) continue;
            try
            {
                await QueueSemaphore.WaitAsync(cancellationToken); // Wait for a thumbnail request
                var id = imageId;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessThumbnailAsync(id, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error creating thumbnail for image {id}: {ex.Message}");
                    }
                    finally
                    {
                        QueueSemaphore.Release();
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Canceled");
            }
        }


        Logger.LogInformation("Thumbnail Service stopped.");
    }

    private async Task ProcessThumbnailAsync(string imageId, CancellationToken cancellationToken)
    {
        try
        {
            // Use your service provider to resolve necessary services (DB access, image processing)
            using var scope = ServiceProvider.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileSystemBusinessLayer>(); // Assumed IImageService handles image fetching

            // Fetch the image from the database
            var fileInfo = fileService.Get(imageId);
            if (fileInfo == null)
            {
                Logger.LogWarning($"File with ID {imageId} not found.");
                return;
            }

            // check if file is an image 
            if (fileInfo.ContentType.IsImageFile())
            {
                await CreateImageThumbnail(fileInfo, fileService, cancellationToken);
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"Error creating thumbnail for image {imageId}: {e.Message}");
        }
    }

    private async Task CreateImageThumbnail(FileInfoModel fileInfo, IFileSystemBusinessLayer fileService, CancellationToken cancellationToken)
    {
        // Load the image from the absolute path
        var imagePath = fileInfo.AbsolutePath;
        var fileId = fileInfo.Id.ToString();
        int attempts = 0;
        int maxRetries = 3;

        if (!File.Exists(imagePath))
        {
            Logger.LogWarning($"File at path {imagePath} does not exist.");
            return;
        }

        while (attempts < maxRetries)
        {
            try
            {
                await using var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
                using var image = await Image.LoadAsync(imageStream, cancellationToken);

                // Define thumbnail size with aspect ratio
                var width = image.Width;
                var height = image.Height;

                if (width > height)
                {
                    height = (int)(height * (MaxDimension / (double)width));
                    width = MaxDimension;
                }
                else
                {
                    width = (int)(width * (MaxDimension / (double)height));
                    height = MaxDimension;
                }

                // Create a thumbnail
                using var thumbnailStream = new MemoryStream();
                using var extendedImage = new MemoryStream();

                await image.SaveAsWebpAsync(extendedImage, cancellationToken);

                image.Mutate(x => x.Resize(width, height)); // Resize with aspect ratio
                await image.SaveAsWebpAsync(thumbnailStream, cancellationToken); // Save as JPEG

                // Define the thumbnail path
                var thumbnailFileName = $"{fileId}_thumb.webp";
                var extendedFileName = $"{fileId}_ext.webp";
                var thumbnailPath = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails", thumbnailFileName);
                var extendImagePath = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails", extendedFileName);

                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

                // Save the thumbnail

                var thumbnailSize = await SaveStream(thumbnailStream, thumbnailPath, cancellationToken);
                var extendedImageSize = await SaveStream(extendedImage, extendImagePath, cancellationToken);

                FileInfoModel thumbnailFile = new FileInfoModel()
                {
                    FileName = thumbnailFileName,
                    AbsolutePath = thumbnailPath,
                    FileSize = thumbnailSize,
                    Type = FileContentType.ThumbnailFile,
                    CreatedDate = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    ContentType = "image/webp",
                    RootFolder = fileInfo.RootFolder
                };

                FileInfoModel extendedFile = new FileInfoModel()
                {
                    FileName = extendedFileName,
                    AbsolutePath = extendImagePath,
                    FileSize = extendedImageSize,
                    Type = FileContentType.ThumbnailWebpFile,
                    CreatedDate = DateTime.Now,
                    ModifiedTime = DateTime.Now,
                    ContentType = "image/webp",
                    RootFolder = fileInfo.RootFolder
                };

                // Update the fileInfo with the thumbnail path
                fileInfo.Thumbnail = thumbnailFile.Id.ToString();
                fileInfo.ExtendResource.Add(new FileContents()
                {
                    Id = extendedFile.Id.ToString(),
                    Type = FileContentType.ThumbnailWebpFile
                });


                await fileService.CreateAsync(thumbnailFile, cancellationToken);
                await fileService.CreateAsync(extendedFile, cancellationToken);
                await fileService.UpdateAsync(fileId, new FieldUpdate<FileInfoModel>()
                {
                    { x => x.Thumbnail, fileInfo.Thumbnail },
                    { x => x.ExtendResource, fileInfo.ExtendResource }
                }, cancellationToken);
            }
            catch (IOException)
            {
                _thumbnailQueue.Add(fileId, cancellationToken);
                await Task.Delay(10000, cancellationToken);
                attempts++;
            }
        }

        if (attempts >= maxRetries) logger.LogError($"[File|{fileId}] Too many retries");
    }


    private async Task<long> SaveStream(Stream stream, string thumbnailPath, CancellationToken cancellationToken = default)
    {
        stream.SeekBeginOrigin(); // Reset stream position
        await using var thumbnailFileStream = new FileStream(thumbnailPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await stream.CopyToAsync(thumbnailFileStream, cancellationToken);
        thumbnailFileStream.SeekBeginOrigin();
        return thumbnailFileStream.Length;
    }

    public void Dispose()
    {
        QueueSemaphore.Dispose();
        _cancellationTokenSource.Dispose();
    }
}