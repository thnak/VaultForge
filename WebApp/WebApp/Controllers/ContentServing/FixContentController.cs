using Business.Business.Interfaces.FileSystem;
using Business.Services.Interfaces;
using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.ContentServing;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public class FixContentController(IFileSystemBusinessLayer fileServe, IFolderSystemBusinessLayer folderServe, IThumbnailService thumbnailService, ILogger<FixContentController> logger) : ControllerBase
{
    [HttpPost("fix-content")]
    public async Task<IActionResult> FixContent()
    {
        var cancelToken = HttpContext.RequestAborted;
        await foreach (var folder in folderServe.GetAllAsync(cancelToken))
        {
            var nameArray = folder.RelativePath.Split('/').ToList();
            var currentIndex = nameArray.FindIndex(f => f == folder.FolderName);
            if (currentIndex == -1)
            {
                logger.LogError($"[{folder.Id}] Folder {folder.FolderName} not found");
                continue;
            }

            string parentRelativePath = nameArray.Count == 1 ? string.Empty : folder.RelativePath.Replace(folder.FolderName, "");

            if (!string.IsNullOrEmpty(parentRelativePath))
            {
                parentRelativePath = parentRelativePath.TrimEnd('/');
                var rootFolder = folderServe.Get(folder.Username, parentRelativePath);
                if (rootFolder == null)
                {
                    logger.LogError($"[{folder.Id}] Folder {folder.FolderName} not found");
                    continue;
                }

                folder.RootFolder = rootFolder.Id.ToString();
            }


            foreach (var folderContent in folder.Contents)
            {
                if (folderContent is { Type: FolderContentType.File or FolderContentType.DeletedFile or FolderContentType.HiddenFile })
                {
                    await foreach (var file in fileServe.GetAllAsync(cancelToken))
                    {
                        await UpdateFile(file, folder, cancelToken);
                    }
                }
            }

            var folderUpdateResult = await folderServe.UpdateAsync(folder, cancelToken);
            if (folderUpdateResult.Item1)
            {
                logger.LogInformation($"[{folder.Id}] Folder {folder.FolderName} updated");
            }
            else
            {
                logger.LogError($"[{folder.Id}] Folder {folder.FolderName} not updated");
            }
        }

        return Ok();
    }

    private async Task UpdateFile(FileInfoModel file, FolderInfoModel folder, CancellationToken cancelToken)
    {
        file.RootFolder = folder.RootFolder;
        file.RelativePath = folder.RelativePath + $"/{file.FileName}";

        if (!string.IsNullOrEmpty(file.Thumbnail))
        {
            var thumbFile = fileServe.Get(file.Thumbnail);
            if (thumbFile != null)
            {
                thumbFile.RootFolder = folder.RootFolder;
                thumbFile.RelativePath = folder.RelativePath + $"/{thumbFile.FileName}";
                await fileServe.UpdateAsync(thumbFile, cancelToken);
                if (!string.IsNullOrEmpty(thumbFile.Thumbnail))
                {
                    await UpdateFile(thumbFile, folder, cancelToken);
                }
            }
        }
        else
        {
            thumbnailService.AddThumbnailRequest(file.Id.ToString());
        }

        var fileUpdateResult = await fileServe.UpdateAsync(file, cancelToken);
        if (fileUpdateResult.Item1)
        {
            logger.LogInformation($"[{file.Id}] File {file.FileName} updated");
        }
        else
        {
            logger.LogError($"[{file.Id}] File {file.FileName} not updated");
        }
    }
}