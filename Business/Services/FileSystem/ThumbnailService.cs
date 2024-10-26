﻿using Business.Business.Interfaces.FileSystem;
using Business.Data;
using Business.Models;
using Business.Services.Interfaces;
using Business.Services.TaskQueueServices.Base.Interfaces;
using BusinessModels.General.EnumModel;
using BusinessModels.General.SettingModels;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Business.Services.FileSystem;

public class ThumbnailService(IParallelBackgroundTaskQueue queue, IOptions<AppSettings> options, IFileSystemBusinessLayer fileService, RedundantArrayOfIndependentDisks raidService, ILogger<ThumbnailService> logger) : IThumbnailService, IDisposable
{
    public Task AddThumbnailRequest(string imageId)
    {
        queue.QueueBackgroundWorkItemAsync(token => ProcessThumbnailAsync(imageId, token));
        return Task.CompletedTask;
    }

    private async ValueTask ProcessThumbnailAsync(string imageId, CancellationToken cancellationToken)
    {
        try
        {
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
                await CreateImageThumbnail(fileInfo, cancellationToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError($"Error creating thumbnail for image {imageId}: {e.Message}");
        }
    }

    private async Task CreateImageThumbnail(FileInfoModel fileInfo, CancellationToken cancellationToken)
    {
        // Load the image from the absolute path
        var imagePath = fileInfo.AbsolutePath;
        var fileId = fileInfo.Id.ToString();
        int attempts = 0;
        int maxRetries = 3;


        if (!raidService.Exists(imagePath))
        {
            logger.LogWarning($"File at path {imagePath} does not exist.");
            return;
        }

        while (attempts < maxRetries)
        {
            try
            {
                // Define the thumbnail path
                var thumbnailFileName = $"{fileId}_thumb.webp";
                var extendedFileName = $"{fileId}_ext.webp";
                var thumbnailPath = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails", thumbnailFileName);
                var extendImagePath = Path.Combine(Path.GetDirectoryName(imagePath)!, "thumbnails", extendedFileName);

                MemoryStream imageStream = new MemoryStream((int)fileInfo.FileSize);
                await raidService.ReadGetDataAsync(imageStream, imagePath, cancellationToken);
                var image = await Image.LoadAsync(imageStream, cancellationToken);
                await imageStream.DisposeAsync();

                // Define thumbnail size with aspect ratio
                var width = image.Width;
                var height = image.Height;

                if (width > height)
                {
                    height = (int)(height * (options.Value.ThumbnailSetting.ImageThumbnailSize / (double)width));
                    width = options.Value.ThumbnailSetting.ImageThumbnailSize;
                }
                else
                {
                    width = (int)(width * (options.Value.ThumbnailSetting.ImageThumbnailSize / (double)height));
                    height = options.Value.ThumbnailSetting.ImageThumbnailSize;
                }

                // Create a thumbnail
                var extendedImage = new MemoryStream();
                await image.SaveAsWebpAsync(extendedImage, cancellationToken);
                var extendedImageSize = await SaveStream(raidService, extendedImage, extendImagePath, cancellationToken);
                await extendedImage.DisposeAsync();

                image.Mutate(x => x.Resize(width, height)); // Resize with aspect ratio

                var thumbnailStream = new MemoryStream();
                await image.SaveAsWebpAsync(thumbnailStream, cancellationToken);
                image.Dispose();
                var thumbnailSize = await SaveStream(raidService, thumbnailStream, thumbnailPath, cancellationToken);
                await thumbnailStream.DisposeAsync();

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
            catch (OutOfMemoryException)
            {
                await Task.Delay(10000, cancellationToken);
                attempts++;
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
        //
    }
}