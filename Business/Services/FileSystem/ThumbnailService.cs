using Business.Business.Interfaces.FileSystem;
using Business.Data;
using Business.Models;
using Business.Services.Interfaces;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Business.Services.FileSystem;

public class ThumbnailService(IParallelBackgroundTaskQueue queue, IServiceProvider serviceProvider, ILogger<ThumbnailService> logger) : IThumbnailService, IDisposable
{
    private const int MaxDimension = 480; // Maximum width or height

    // To resolve database and image services
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Task AddThumbnailRequest(string imageId)
    {
        queue.QueueBackgroundWorkItemAsync(token => ProcessThumbnailAsync(imageId, token));
        return Task.CompletedTask;
    }

    private async ValueTask ProcessThumbnailAsync(string imageId, CancellationToken cancellationToken)
    {
        try
        {
            // Use your service provider to resolve necessary services (DB access, image processing)
            using var scope = serviceProvider.CreateScope();
            var fileService = scope.ServiceProvider.GetRequiredService<IFileSystemBusinessLayer>(); // Assumed IImageService handles image fetching

            // Fetch the image from the database
            var fileInfo = fileService.Get(imageId);
            if (fileInfo == null)
            {
                logger.LogWarning($"File with ID {imageId} not found.");
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
            logger.LogError($"Error creating thumbnail for image {imageId}: {e.Message}");
        }
    }

    private async Task CreateImageThumbnail(FileInfoModel fileInfo, IFileSystemBusinessLayer fileService, CancellationToken cancellationToken)
    {
        // Load the image from the absolute path
        var imagePath = fileInfo.AbsolutePath;
        var fileId = fileInfo.Id.ToString();
        int attempts = 0;
        int maxRetries = 3;

        var raidService = serviceProvider.CreateScope().ServiceProvider.GetService<RedundantArrayOfIndependentDisks>()!;

        if (!raidService.Exists(imagePath))
        {
            logger.LogWarning($"File at path {imagePath} does not exist.");
            return;
        }

        while (attempts < maxRetries)
        {
            try
            {
                using MemoryStream imageStream = new MemoryStream((int)fileInfo.FileSize);
                await raidService.ReadGetDataAsync(imageStream, imagePath, cancellationToken);
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

                var thumbnailSize = await SaveStream(raidService, thumbnailStream, thumbnailPath, cancellationToken);
                var extendedImageSize = await SaveStream(raidService, extendedImage, extendImagePath, cancellationToken);

                FileInfoModel thumbnailFile = new FileInfoModel()
                {
                    FileName = thumbnailFileName,
                    AbsolutePath = thumbnailPath,
                    FileSize = thumbnailSize,
                    Classify = FileClassify.ThumbnailFile,
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
                    Classify = FileClassify.ThumbnailWebpFile,
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
                    Classify = FileClassify.ThumbnailWebpFile
                });


                await fileService.CreateAsync(thumbnailFile, cancellationToken);
                await fileService.CreateAsync(extendedFile, cancellationToken);
                await fileService.UpdateAsync(fileId, new FieldUpdate<FileInfoModel>()
                {
                    { x => x.Thumbnail, fileInfo.Thumbnail },
                    { x => x.ExtendResource, fileInfo.ExtendResource }
                }, cancellationToken);
                break;
            }
            catch (IOException)
            {
                await Task.Delay(10000, cancellationToken);
                attempts++;
            }
            catch (MongoException)
            {
                logger.LogWarning($"File with ID {fileId} already exists.");
            }
        }

        if (attempts >= maxRetries) logger.LogError($"[File|{fileId}] Too many retries");
    }

    private async Task<long> SaveStream(RedundantArrayOfIndependentDisks service, Stream stream, string thumbnailPath, CancellationToken cancellationToken = default)
    {
        stream.SeekBeginOrigin(); // Reset stream position
        var result = await service.WriteDataAsync(stream, thumbnailPath, cancellationToken);
        return result.TotalByteWritten;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}