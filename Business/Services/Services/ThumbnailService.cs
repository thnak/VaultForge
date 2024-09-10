using Business.Business.Interfaces.FileSystem;
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

public class ThumbnailService(IServiceProvider serviceProvider, ILogger<ThumbnailService> logger) : IThumbnailService
{
    private readonly Queue<string> _thumbnailQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(0);
    private const int MaxDimension = 480; // Maximum width or height


    // To resolve database and image services
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public void AddThumbnailRequest(string imageId)
    {
        lock (_thumbnailQueue)
        {
            _thumbnailQueue.Enqueue(imageId);
        }

        _queueSemaphore.Release(); // Signal that a new request is available
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Thumbnail Service started.");

        // Continue processing until the app shuts down or cancellation is requested
        while (!cancellationToken.IsCancellationRequested)
        {
            await _queueSemaphore.WaitAsync(cancellationToken); // Wait for a thumbnail request

            string imageId;
            lock (_thumbnailQueue)
            {
                if (_thumbnailQueue.Count == 0) continue;
                imageId = _thumbnailQueue.Dequeue();
            }

            // Process the thumbnail creation
            try
            {
                await ProcessThumbnailAsync(imageId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error creating thumbnail for image {imageId}: {ex.Message}");
            }
        }

        logger.LogInformation("Thumbnail Service stopped.");
    }

    private async Task ProcessThumbnailAsync(string imageId, CancellationToken cancellationToken)
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
        if (!File.Exists(imagePath))
        {
            logger.LogWarning($"File at path {imagePath} does not exist.");
            return;
        }

        await using var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
        image.Mutate(x => x.Resize(width, height)); // Resize with aspect ratio

        await image.SaveAsWebpAsync(thumbnailStream, cancellationToken); // Save as JPEG

        // Define the thumbnail path
        var thumbnailFileName = $"{fileInfo.FileName}_thumb.webp";
        var thumbnailPath = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails", thumbnailFileName);

        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);

        // Save the thumbnail
        thumbnailStream.SeekBeginOrigin(); // Reset stream position
        await using var thumbnailFileStream = new FileStream(thumbnailPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await thumbnailStream.CopyToAsync(thumbnailFileStream, cancellationToken);
        thumbnailFileStream.SeekBeginOrigin();


        FileInfoModel thumbnailFile = new FileInfoModel()
        {
            FileName = thumbnailFileName,
            AbsolutePath = thumbnailPath,
            FileSize = thumbnailFileStream.Length,
            Type = FileContentType.ThumbnailFile,
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            ContentType = "image/webp"
        };

        // Update the fileInfo with the thumbnail path
        fileInfo.Thumbnail = thumbnailFile.Id.ToString();

        await fileService.CreateAsync(thumbnailFile, cancellationToken);
        await fileService.UpdateAsync(fileInfo, cancellationToken); // Save updated file info to DB

        // use image

        logger.LogInformation($"Thumbnail created for image {fileInfo.Id}.");
    }


    public void Stop()
    {
        _cancellationTokenSource.Cancel(); // Stop the background process
    }
}