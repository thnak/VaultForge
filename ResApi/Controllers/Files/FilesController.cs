using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Data.StorageSpace;
using Business.Models;
using Business.Services.Configure;
using Business.Services.Interfaces;
using Business.Services.TaskQueueServices.Base.Interfaces;
using Business.Utils.Helper;
using BusinessModels.General.EnumModel;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Protector.Utils;

namespace ResApi.Controllers.Files;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public class FilesController(
    IFileSystemBusinessLayer fileServe,
    IFolderSystemBusinessLayer folderServe,
    IThumbnailService thumbnailService,
    ILogger<FilesController> logger,
    RedundantArrayOfIndependentDisks raidService,
    IParallelBackgroundTaskQueue parallelBackgroundTaskQueue,
    ApplicationConfiguration options) : ControllerBase
{
    [HttpGet("get-file-wall-paper")]
    [IgnoreAntiforgeryToken]
    [OutputCache(NoStore = true)]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> GetWallPaper()
    {
        var cancelToken = HttpContext.RequestAborted;

        var anonymousUser = "Anonymous".ComputeSha256Hash();

        var rootWallpaperFolder = folderServe.Get(anonymousUser, "/root/wallpaper");
        if (rootWallpaperFolder == null)
            return NotFound();


        var file = await fileServe.GetRandomFileAsync(rootWallpaperFolder.Id.ToString(), cancelToken);
        if (file == null) return NotFound();

        var fileThumbnail = await fileServe.GetSubFileByClassifyAsync(file.Id.ToString(), FileClassify.ThumbnailWebpFile, cancelToken);
        if (fileThumbnail != null)
        {
            file = fileThumbnail;
        }

        var memoryStream = new MemoryStream();
        await raidService.ReadGetDataAsync(memoryStream, file.AbsolutePath, cancelToken);
        memoryStream.SeekBeginOrigin();


        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = HttpUtility.UrlEncode(file.FileName),
            Inline = true, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };

        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.RegisterForDispose(memoryStream);
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;

        return File(memoryStream, file.ContentType);
    }

    [HttpPost("insert-media")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> InsertContent([FromForm] string path)
    {
        var result = await folderServe.InsertMediaContent(path);
        if (result.IsSuccess)
        {
            return Ok(result.Message);
        }

        return BadRequest(result.Message);
    }

    // [HttpGet("get-file")]
    // [IgnoreAntiforgeryToken]
    // public async Task<IActionResult> GetFile(string id)
    // {
    //     var cancelToken = HttpContext.RequestAborted;
    //     id = id.Split(".").First();
    //     var file = fileServe.GetSubFileByClassifyAsync(id, cancelToken, []);
    //     if (file == null) return NotFound();
    //
    //     var webpImageContent = file.ParentResource.FirstOrDefault(x => x is { Classify: FileClassify.M3U8File or FileClassify.M3U8FileSegment or FileClassify.ThumbnailWebpFile });
    //     if (webpImageContent != null)
    //     {
    //         var resId = webpImageContent.LogId.Split(".").First();
    //         var webpImage = fileServe.Get(resId);
    //         if (webpImage != null)
    //         {
    //             file = webpImage;
    //         }
    //     }
    //
    //     var now = DateTime.UtcNow;
    //     var cd = new ContentDisposition
    //     {
    //         FileName = HttpUtility.UrlEncode(file.FileName),
    //         Inline = true, // false = prompt the user for downloading;  true = browser to try to show the file inline,
    //         CreationDate = now,
    //         ModificationDate = now,
    //         ReadDate = now
    //     };
    //     Response.Headers.Append("Content-Disposition", cd.ToString());
    //     Response.ContentType = file.ContentType;
    //     Response.Headers.ContentType = file.ContentType;
    //     Response.StatusCode = 200;
    //     Response.ContentLength = file.FileSize;
    //
    //     MemoryStream ms = new MemoryStream();
    //     Response.RegisterForDispose(ms);
    //
    //     await raidService.ReadGetDataAsync(ms, file.AbsolutePath, cancelToken);
    //
    //     if (file is { Classify: FileClassify.M3U8File })
    //     {
    //         var streamReader = new StreamReader(ms);
    //         Response.RegisterForDispose(streamReader);
    //         string[] lines = (await streamReader.ReadToEndAsync(cancelToken)).Split("\n");
    //         ms.Seek(0, SeekOrigin.Begin);
    //         for (var i = 0; i < lines.Length; i++)
    //         {
    //             var line = lines[i];
    //             if (line.Contains(".m3u8") || line.Contains(".ts") || line.Contains(".vtt"))
    //             {
    //                 lines[i] = $"{HttpContext.Request.Scheme}://" + HttpContext.Request.Host.Value + HttpContext.Request.Path + "?id=" + line;
    //                 lines[i] = lines[i].Trim();
    //             }
    //         }
    //
    //         var stringContent = string.Join("\n", lines);
    //         return Content(stringContent, file.ContentType);
    //     }
    //
    //     return new FileStreamResult(ms, file.ContentType)
    //     {
    //         FileDownloadName = file.FileName,
    //         LastModified = file.ModifiedTime,
    //         EnableRangeProcessing = true
    //     };
    // }

    [HttpGet("download-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DownloadFile(string id)
    {
        var cancelToken = HttpContext.RequestAborted;
        var file = fileServe.Get(id);
        if (file == null) return NotFound(AppLang.File_not_found_);
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = HttpUtility.UrlEncode(file.FileName),
            Inline = false, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };
        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.ContentType = file.ContentType;
        Response.Headers.ContentType = file.ContentType;
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;
        MemoryStream ms = new MemoryStream();
        await raidService.ReadGetDataAsync(ms, file.AbsolutePath, cancelToken);
        Response.RegisterForDispose(ms);

        using SHA256 sha256 = SHA256.Create();

        int bytesRead1;
        byte[] buffer = new byte[1024];
        while ((bytesRead1 = await ms.ReadAsync(buffer, 0, 1024, cancelToken)) > 0)
        {
            sha256.TransformBlock(buffer, 0, bytesRead1, null, 0);
        }

        ms.Seek(0, SeekOrigin.Begin);

        sha256.TransformFinalBlock([], 0, 0);
        StringBuilder checksum = new StringBuilder();
        if (sha256.Hash != null)
        {
            foreach (byte b in sha256.Hash)
            {
                checksum.Append(b.ToString("x2"));
            }
        }

        if (file.Checksum == checksum.ToString())
        {
            return new FileStreamResult(ms, file.ContentType)
            {
                FileDownloadName = file.FileName,
                LastModified = file.ModifiedTime,
                EnableRangeProcessing = false
            };
        }

        return BadRequest();
    }

    [HttpGet("stream-raid")]
    public async Task<IActionResult> StreamRaidVideo(string path)
    {
        var cancelToken = HttpContext.RequestAborted;
        var file = fileServe.Get(path);
        if (file == null) return NotFound(AppLang.File_not_found_);

        var pathArray = await raidService.GetDataBlockPaths(file.AbsolutePath, cancelToken);
        if (pathArray == default) return NotFound();

        // Check if Range request header exists
        if (Request.Headers.ContainsKey("Range"))
        {
            // Parse the Range header
            var rangeHeader = Request.Headers["Range"].ToString();
            var range = rangeHeader.Replace("bytes=", "").Split('-');

            long from = long.Parse(range[0]);
            long to = range.Length > 1 && long.TryParse(range[1], out var endRange) ? endRange : file.FileSize - 1;

            if (from >= file.FileSize)
            {
                return BadRequest("Requested range is not satisfiable.");
            }

            var length = (int)(to - from + 1);

            byte[] buffer = new byte[length];

            Raid5Stream raid5Stream = new Raid5Stream(pathArray.Files[0], pathArray.Files[1], pathArray.Files[2], pathArray.FileSize, pathArray.StripeSize);
            raid5Stream.Seek(from, SeekOrigin.Begin);

            _ = await raid5Stream.ReadAsync(buffer, 0, length, cancelToken);
            await raid5Stream.DisposeAsync();
            // Set headers for partial content response
            Response.Headers.Append("Content-Range", $"bytes {from}-{to}/{file.FileSize}");
            Response.Headers.Append("Accept-Ranges", "bytes");
            Response.ContentLength = length;
            Response.StatusCode = StatusCodes.Status206PartialContent;

            return File(buffer, "video/mp4");
        }

        return NotFound("Unsupported");
    }


    [HttpPost("get-file-list")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFiles([FromBody] List<string> listFiles)
    {
        List<FileInfoModel> files = [];
        var cancelToken = HttpContext.RequestAborted;

        foreach (var id in listFiles.TakeWhile(_ => cancelToken is not { IsCancellationRequested: true }))
        {
            var file = fileServe.Get(id);
            if (file == null) continue;
            file.AbsolutePath = string.Empty;
            files.Add(file);
        }

        return Content(files.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpPost("get-folder-list")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFolderList([FromBody] List<string> listFolders)
    {
        List<FolderInfoModel> files = [];
        var cancelToken = HttpContext.RequestAborted;
        files.AddRange(listFolders.TakeWhile(_ => cancelToken is not { IsCancellationRequested: true }).Select(folderServe.Get).OfType<FolderInfoModel>());

        return Content(files.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("get-file-v2")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFile_v2(string id)
    {
        var file = fileServe.Get(id);
        if (file == null) return NotFound();
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = file.FileName,
            Inline = true, // false = prompt the user for downloading;  true = browser to try to show the file inline,
            CreationDate = now,
            ModificationDate = now,
            ReadDate = now
        };
        Response.Headers.Append("Content-Disposition", cd.ToString());
        Response.ContentType = file.ContentType;
        Response.Headers.ContentType = file.ContentType;
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;
        return PhysicalFile(file.AbsolutePath, file.ContentType, true);
    }

    [HttpPost("{username}/get-folder")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10)]
    [ResponseCache(Duration = 50)]
    public async Task<IActionResult> GetSharedFolder(string username, [FromForm] string? id, [FromForm] string? password,
        [FromForm] int page, [FromForm] int pageSize, [FromForm] string? contentTypes, [FromForm] bool? forceReLoad, [FromForm] bool? allowSystemResoure)
    {
        var cancelToken = HttpContext.RequestAborted;
        try
        {
            var folderSource = string.IsNullOrEmpty(id) ? folderServe.GetRoot(username) : folderServe.Get(id);
            if (folderSource == null) return BadRequest(AppLang.Folder_could_not_be_found);
            if (!string.IsNullOrEmpty(folderSource.Password))
            {
                if (password == null || password.ComputeSha256Hash() != folderSource.Password)
                    return Unauthorized(AppLang.This_resource_is_protected_by_password);
            }

            folderSource.Password = string.Empty;
            folderSource.OwnerUsername = string.Empty;

            List<FolderContentType> contentFolderTypesList = [];

            if (!string.IsNullOrEmpty(contentTypes))
            {
                var listString = contentTypes.DeSerialize<FolderContentType[]>();
                if (listString != null) contentFolderTypesList = listString.ToList();
            }
            else
            {
                contentFolderTypesList = [FolderContentType.Folder];
            }

            if (!contentFolderTypesList.Any(x => x is FolderContentType.DeletedFolder))
                contentFolderTypesList.Add(FolderContentType.SystemFolder);

            var contentFileTypesList = contentFolderTypesList.Select(x => x.MapFileContentType()).Distinct().ToList();

            FileClassify[] fileClassify = [FileClassify.Normal];

            string rootFolderId = folderSource.Id.ToString();
            var res = await folderServe.GetFolderRequestAsync(rootFolderId,
                folderInfoModel => folderInfoModel.RootFolder == rootFolderId && contentFolderTypesList.Contains(folderInfoModel.Type),
                fileInfoModel => fileInfoModel.RootFolder == rootFolderId && contentFileTypesList.Contains(fileInfoModel.Status) && fileClassify.Contains(fileInfoModel.Classify),
                pageSize, page, forceReLoad is true, cancelToken);

            res.Folder = folderSource;
            return Content(res.ToJson(), MediaTypeNames.Application.Json);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request cancelled");
        }

        return Ok();
    }

    [HttpPost("get-deleted-content")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10)]
    [ResponseCache(Duration = 50)]
    public async Task<IActionResult> GetDeletedContent([FromForm] string? userName, [FromForm] int pageSize, [FromForm] int page)
    {
        try
        {
            var cancelToken = HttpContext.RequestAborted;
            var content = await folderServe.GetDeletedContentAsync(userName, pageSize, page, cancellationToken: cancelToken);
            return Content(content.ToJson(), MediaTypeNames.Application.Json);
        }
        catch (OperationCanceledException)
        {
            return Ok();
        }
    }

    [HttpPost("search-folder")]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10, NoStore = true)]
    public async Task<IActionResult> SearchFolder([FromForm] string? username, [FromForm] string searchString)
    {
        List<FolderInfoModel> folderList = [];
        var cancelToken = HttpContext.RequestAborted;

        var user = string.IsNullOrEmpty(username) ? folderServe.GetUser(string.Empty) : new UserModel();

        try
        {
            await foreach (var x in folderServe.Where(x => x.FolderName.Contains(searchString) ||
                                                           x.RelativePath.Contains(searchString) &&
                                                           (user == null || x.OwnerUsername == user.UserName) &&
                                                           x.Type == FolderContentType.Folder, cancelToken,
                               model => model.FolderName, model => model.Type, model => model.Icon, model => model.ModifiedTime, model => model.Id))
            {
                folderList.Add(x);
                if (folderList.Count == 10)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            //
        }
        catch (Exception)
        {
            //
        }

        return Content(folderList.ToJson(), MimeTypeNames.Application.Json);
    }

    [HttpGet("get-folder-blood-line")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFolderBloodLine(string id)
    {
        var folders = folderServe.GetFolderBloodLine(id);
        return Content(folders.ToJson(), MimeTypeNames.Application.Json);
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


    [HttpPost("re-name-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReNameFile([FromForm] string objectId, [FromForm] string newName)
    {
        var file = fileServe.Get(objectId);
        if (file == null) return BadRequest(AppLang.File_not_found_);
        if (string.IsNullOrEmpty(newName))
            return BadRequest(AppLang.ThisFieldIsRequired);
        file.FileName = newName;
        var status = await fileServe.UpdateAsync(file);
        return status.Item1 ? Ok(status.Item2) : BadRequest(status.Item2);
    }

    [HttpPost("re-name-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ReNameFolder([FromForm] string objectId, [FromForm] string newName)
    {
        var folder = folderServe.Get(objectId);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);
        if (string.IsNullOrEmpty(newName))
            return BadRequest(AppLang.ThisFieldIsRequired);

        var rootFolder = folderServe.GetRoot(folder.RootFolder);
        if (rootFolder == default)
            return BadRequest();

        folder.FolderName = newName;
        folder.RelativePath = rootFolder.RelativePath + "/" + newName;

        var status = await folderServe.UpdateAsync(folder);
        return status.Item1 ? Ok(status.Item2) : BadRequest(status.Item2);
    }

    [HttpPost("restore-content")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> RestoreContent([FromForm] string id, [FromForm] bool isFile)
    {
        if (isFile)
        {
            var file = fileServe.Get(id);
            if (file == null) return BadRequest(AppLang.File_not_found_);
            var result = await fileServe.UpdateAsync(id, new FieldUpdate<FileInfoModel>() { { model => model.Status, file.PreviousStatus } });
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }

        {
            var folder = folderServe.Get(id);
            if (folder == null) return BadRequest(AppLang.File_not_found_);
            var result = await folderServe.UpdateAsync(id, new FieldUpdate<FolderInfoModel>() { { model => model.Type, folder.PreviousType } });
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }
    }

    [HttpDelete("delete-file")]
    public async Task<IActionResult> DeleteFile([FromForm] string fileId, [FromForm] string folderId)
    {
        var file = fileServe.Get(fileId);
        if (file == null) return BadRequest(AppLang.File_not_found_);

        var folder = folderServe.Get(folderId);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);

        var fileDeleteStatus = await fileServe.DeleteAsync(fileId);
        if (fileDeleteStatus.Item1)
        {
            await folderServe.UpdateAsync(folder);
        }

        return BadRequest(fileDeleteStatus.Item2);
    }

    [HttpDelete("safe-delete-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFile(string code)
    {
        var result = await fileServe.DeleteAsync(code);
        return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
    }

    [HttpDelete("safe-delete-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFolder(string code)
    {
        var updateResult = await folderServe.DeleteAsync(code);
        return updateResult.Item1 ? Ok(updateResult.Item2) : BadRequest(updateResult.Item2);
    }


    [HttpPut("create-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateSharedFolder([FromBody] RequestNewFolderModel request)
    {
        var result = await folderServe.CreateFolder(request);
        return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
    }


    [HttpPut("move-file-to-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MoveFile2([FromForm] List<string> fileCodes, [FromForm] string currentFolderCode, [FromForm] string targetFolderCode, [FromForm] string? password)
    {
        var cancelToken = HttpContext.RequestAborted;
        var currentFolder = folderServe.Get(currentFolderCode);
        if (currentFolder == null) return NotFound(AppLang.Current_folder_could_not_have_found);

        var targetFolder = folderServe.Get(targetFolderCode);
        if (targetFolder == null) return NotFound(AppLang.Target_folder_could_not_have_found);


        if (!string.IsNullOrEmpty(currentFolder.Password))
        {
            if (!string.IsNullOrEmpty(password))
            {
                if (currentFolder.Password != password.ComputeSha256Hash())
                    return BadRequest(AppLang.Passwords_do_not_match_);
            }
            else
            {
                return BadRequest(AppLang.Incorrect_password);
            }
        }

        var files = fileCodes.Select(fileServe.Get).Where(x => x != default).ToList();
        
        foreach (var file in files)
        {
            if (file == default)
            {
                ModelState.AddModelError(AppLang.File, AppLang.File_not_found_);
                continue;
            }

            var fileName = file.RelativePath.Split("/").Last();
            file.RelativePath = targetFolder.RelativePath + '/' + fileName;

            var fileId = file.Id.ToString();
        }


        await folderServe.UpdateAsync(targetFolder, cancelToken);
        await folderServe.UpdateAsync(currentFolder, cancelToken);
        await foreach (var x in fileServe.UpdateAsync(files!, cancelToken))
            if (!x.Item1)
                ModelState.AddModelError(AppLang.File, x.Item2);


        return Ok(ModelState.Any() ? ModelState : AppLang.File_moved_successfully);
    }

    [HttpPut("move-folder-to-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MoveFolder2Folder([FromForm] string folderCode, [FromForm] string targetFolderCode)
    {
        var targetFolder = folderServe.Get(targetFolderCode);
        if (targetFolder == null) return NotFound(AppLang.Target_folder_could_not_have_found);

        var folder = folderServe.Get(folderCode);
        if (folder == default) return NotFound(AppLang.Folder_could_not_be_found);

        folder.RelativePath = targetFolder.RelativePath + '/' + folder.FolderName;

        await folderServe.UpdateAsync(targetFolder);
        await folderServe.UpdateAsync(folder);
        return Ok(AppLang.Folder_moved_successfully);
    }

    [HttpGet("fix-content-status")]
    public async Task<IActionResult> FixFile()
    {
        var cancelToken = HttpContext.RequestAborted;
        var totalFiles = await fileServe.GetDocumentSizeAsync(cancelToken);
        var files = fileServe.Where(x => true, cancelToken, model => model.Id, model => model.PreviousStatus, model => model.Status);

        long index = 0;
        await foreach (var x in files)
        {
            await fileServe.UpdateAsync(x.Id.ToString(), new FieldUpdate<FileInfoModel>()
            {
                { z => z.Status, x.PreviousStatus }
            }, cancelToken);
            index += 1;
            if (index == totalFiles)
                break;
        }

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