using Business.Data.StorageSpace;
using Business.Models;
using Business.Utils.Helper;
using BusinessModels.General.EnumModel;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using WebApp.Attribute;

namespace WebApp.Controllers.ContentServing;

public partial class FilesController
{
    [HttpPut("create-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateSharedFolder([FromBody] RequestNewFolderModel request)
    {
        var result = await folderServe.CreateFolder(request);
        return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
    }

    [HttpPost("insert-media")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> InsertContent([FromForm] string path)
    {
        var result = await folderServe.InsertMediaContent(path);
        if (result.IsSuccess)
            return Ok(result.Message);
        return BadRequest(result.Message);
    }

    [HttpPost("{folderCode}/upload-via-link")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> ImportMovieResource(string folderCode, [FromForm] string url)
    {
        await parallelBackgroundTaskQueue.QueueBackgroundWorkItemAsync(async (token) =>
        {
            var folder = folderServe.Get(folderCode);
            if (folder == null) return;

            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url, token);

            string fileName = response.Content.Headers.GetFileNameFromHeaders() ?? url.GetFileNameFromUrl();

            var file = new FileInfoModel()
            {
                RootFolder = folder.Id.ToString(),
                FileName = fileName,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            };
            await folderServe.CreateFileAsync(folder.OwnerUsername, file, token);
            var stream = await response.Content.ReadAsStreamAsync(token);
            await raidService.WriteDataAsync(stream, file.AbsolutePath, token);
            await fileServe.CreateAsync(file, token);
        });

        return Ok();
    }

    [HttpPost("upload-physical/{folderCodes}")]
    [DisableFormValueModelBinding]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadPhysical(string folderCodes)
    {
        string folderKeyString = AppLang.Folder;
        string fileKeyString = AppLang.File;
        var cancellationToken = HttpContext.RequestAborted;

        try
        {
            // Validate request
            if (!IsValidRequest(out IActionResult? errorResult))
            {
                return errorResult ?? BadRequest();
            }

            // Validate folder
            var folder = folderServe.Get(folderCodes);
            if (folder == null)
            {
                ModelState.AddModelError(folderKeyString, AppLang.Folder_could_not_be_found);
                return BadRequest(ModelState);
            }

            if (folder.Type == FolderContentType.DeletedFolder)
                return BadRequest("Folder deleted");

            var boundary = MediaTypeHeaderValue.Parse(Request.ContentType).GetBoundary(int.MaxValue);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body, options.GetStorage.BufferSize);
            var section = await reader.ReadNextSectionAsync(cancellationToken);

            while (section != null && !cancellationToken.IsCancellationRequested)
            {
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
                {
                    if (contentDisposition.HasFileContentDisposition())
                    {
                        var trustedFileNameForDisplay = contentDisposition.FileName.Value ?? Path.GetRandomFileName();
                        await ProcessFileSection(folderCodes, section, trustedFileNameForDisplay, cancellationToken);
                    }
                    else
                    {
                        ModelState.AddModelError(fileKeyString, "The request couldn't be processed (Error 2).");
                    }
                }

                section = await reader.ReadNextSectionAsync(cancellationToken);
            }

            return ModelState.IsValid ? Ok(AppLang.Successfully_uploaded) : BadRequest(ModelState);
        }
        catch (OperationCanceledException ex)
        {
            return Ok(ex.Message);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(AppLang.Exception, ex.Message);
            logger.LogError(ex, null);
            return StatusCode(500, ModelState);
        }
    }

    #region upload-physical/{folderCodes}

    private bool IsValidRequest(out IActionResult? errorResult)
    {
        string fileKeyString = AppLang.File;

        if (string.IsNullOrEmpty(Request.ContentType) || !MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
        {
            ModelState.AddModelError(fileKeyString, "The request couldn't be processed (Error 1).");
            errorResult = BadRequest(ModelState);
            return false;
        }

        errorResult = null;
        return true;
    }

    private async Task ProcessFileSection(string folderCodes, MultipartSection section, string trustedFileNameForDisplay, CancellationToken cancellationToken)
    {
        var file = new FileInfoModel { FileName = trustedFileNameForDisplay };
        try
        {
            var folder = folderServe.Get(folderCodes);
            if (folder == null)
            {
                return;
            }

            var createFileResult = await folderServe.CreateFileAsync(folder, file, cancellationToken);

            if (!createFileResult.Item1)
            {
                await DeleteFileAsync(file.Id.ToString());
                return;
            }

            await ProcessImageFileSection(section, file, cancellationToken, trustedFileNameForDisplay);
        }
        catch (Exception)
        {
            await DeleteFileAsync(file.Id.ToString());
        }
    }

    private async Task ProcessImageFileSection(MultipartSection section, FileInfoModel file, CancellationToken cancellationToken, string trustedFileNameForDisplay)
    {
        try
        {
            var saveResult = await raidService.WriteDataAsync(section.Body, file.AbsolutePath, cancellationToken);

            await UpdateFileProperties(file, saveResult, string.IsNullOrEmpty(section.ContentType) ? saveResult.ContentType : section.ContentType, trustedFileNameForDisplay);
            if (file.FileSize <= 0)
            {
                await DeleteFileAsync(file.Id.ToString());
                logger.LogInformation("Image file are empty");
            }
        }
        catch (OperationCanceledException)
        {
            await DeleteFileAsync(file.Id.ToString());
            logger.LogInformation("Image file are empty");
        }
    }

    private async Task UpdateFileProperties(FileInfoModel file, RedundantArrayOfIndependentDisks.WriteDataResult saveResult, string contentType, string trustedFileNameForDisplay)
    {
        file.FileSize = saveResult.TotalByteWritten;
        file.ContentType = saveResult.ContentType;
        file.Checksum = saveResult.CheckSum;

        if (string.IsNullOrEmpty(file.ContentType))
        {
            file.ContentType = contentType;
        }
        else if (file.ContentType == "application/octet-stream")
        {
            file.ContentType = Path.GetExtension(trustedFileNameForDisplay).GetMimeTypeFromExtension();
        }

        var updateResult = await fileServe.UpdateAsync(file.Id.ToString(), GetFileFieldUpdates(file));
        await thumbnailService.AddThumbnailRequest(file.Id.ToString());

        if (!updateResult.Item1)
        {
            logger.LogError(updateResult.Item2);
        }
    }

    private static FieldUpdate<FileInfoModel> GetFileFieldUpdates(FileInfoModel file)
    {
        return new FieldUpdate<FileInfoModel>
        {
            { x => x.FileSize, file.FileSize },
            { x => x.ContentType, file.ContentType },
            { x => x.Checksum, file.Checksum }
        };
    }

    private async Task DeleteFileAsync(string fileId)
    {
        await fileServe.DeleteAsync(fileId);
        await fileServe.DeleteAsync(fileId);
    }

    #endregion
}