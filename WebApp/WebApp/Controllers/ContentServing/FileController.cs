using System.Net.Mime;
using System.Web;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Services.Interfaces;
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

namespace WebApp.Controllers.ContentServing;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public class FilesController(IFileSystemBusinessLayer fileServe, IFolderSystemBusinessLayer folderServe, IThumbnailService thumbnailService, ILogger<FilesController> logger) : ControllerBase
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


        var webpImageContent = file.ExtendResource.FirstOrDefault(x => x.Type == FileContentType.ThumbnailWebpFile);
        if (webpImageContent != null)
        {
            var webpImage = fileServe.Get(webpImageContent.Id);
            if (webpImage != null)
            {
                file = webpImage;
            }
        }

        if (!global::System.IO.File.Exists(file.AbsolutePath))
        {
            logger.LogError($"File {file.AbsolutePath} not found");
            return NotFound();
        }

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
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;

        return PhysicalFile(file.AbsolutePath, file.ContentType, true);
    }

    [HttpGet("get-file")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFile(string id)
    {
        var file = fileServe.Get(id);
        if (file == null) return NotFound();

        var webpImageContent = file.ExtendResource.FirstOrDefault(x => x.Type == FileContentType.ThumbnailWebpFile);
        if (webpImageContent != null)
        {
            var webpImage = fileServe.Get(webpImageContent.Id);
            if (webpImage != null)
            {
                file = webpImage;
            }
        }

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
        Response.ContentType = file.ContentType;
        Response.Headers.ContentType = file.ContentType;
        Response.StatusCode = 200;
        Response.ContentLength = file.FileSize;
        return PhysicalFile(file.AbsolutePath, file.ContentType, true);
    }

    [HttpGet("download-file")]
    [IgnoreAntiforgeryToken]
    public IActionResult DownloadFile(string id)
    {
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
        return PhysicalFile(file.AbsolutePath, file.ContentType, true);
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

    [HttpPost("get-folder")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10)]
    [ResponseCache(Duration = 50)]
    public async Task<IActionResult> GetSharedFolder([FromForm] string? id, [FromForm] string? password,
        [FromForm] int page, [FromForm] int pageSize, [FromForm] string? contentTypes)
    {
        var cancelToken = HttpContext.RequestAborted;
        try
        {
            var folderSource = string.IsNullOrEmpty(id) ? folderServe.GetRoot("") : folderServe.Get(id);
            if (folderSource == null) return BadRequest(AppLang.Folder_could_not_be_found);
            if (!string.IsNullOrEmpty(folderSource.Password))
            {
                if (password == null || password.ComputeSha256Hash() != folderSource.Password)
                    return Unauthorized(AppLang.This_resource_is_protected_by_password);
            }

            folderSource.Password = string.Empty;
            folderSource.Username = string.Empty;

            List<FolderContentType> contentFolderTypesList = [];

            if (!string.IsNullOrEmpty(contentTypes))
            {
                var listString = contentTypes.DeSerialize<FolderContentType[]>();
                if (listString != null) contentFolderTypesList = listString.ToList();
            }
            else
            {
                contentFolderTypesList = [FolderContentType.File, FolderContentType.Folder];
            }

            contentFolderTypesList.Add(FolderContentType.SystemFolder);
            var contentFileTypesList = contentFolderTypesList.Select(x => x.MapFileContentType()).Distinct().ToList();
            string rootFolderId = folderSource.Id.ToString();
            var res = await folderServe.GetFolderRequestAsync(model => model.RootFolder == rootFolderId && contentFolderTypesList.Contains(model.Type), model => model.RootFolder == rootFolderId && contentFileTypesList.Contains(model.Type), pageSize, page, cancelToken);
            res.Folder = folderSource;
            return Content(res.ToJson(), MediaTypeNames.Application.Json);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request cancelled");
        }

        return Ok();
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
                                                           (user == null || x.Username == user.UserName) &&
                                                           x.Type == FolderContentType.Folder, cancelToken,
                               model => model.FolderName, model => model.Type, model => model.Icon, model => model.ModifiedTime, model => model.Id))
            {
                folderList.Add(x);
                if (folderList.Count == 10)
                    break;
            }
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

    [HttpDelete("delete-file")]
    public async Task<IActionResult> DeleteFile([FromForm] string fileId, [FromForm] string folderId)
    {
        var file = fileServe.Get(fileId);
        if (file == null) return BadRequest(AppLang.File_not_found_);

        var folder = folderServe.Get(folderId);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);

        var fileDeleteStatus = fileServe.Delete(fileId);
        if (fileDeleteStatus.Item1)
        {
            folder.Contents = folder.Contents.Where(x => x.Id != fileId).ToList();
            await folderServe.UpdateAsync(folder);
        }

        return BadRequest(fileDeleteStatus.Item2);
    }

    [HttpDelete("safe-delete-file")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFile(string code)
    {
        var cancelToken = HttpContext.RequestAborted;
        var file = fileServe.Get(code);
        if (file == null) return NotFound(AppLang.File_not_found_);
        
        if (file is { Type: FileContentType.File or FileContentType.HiddenFile })
        {
            file.Type = FileContentType.DeletedFile;
            var result = await fileServe.UpdateAsync(file, cancelToken);
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }
        else
        {
            var result = fileServe.Delete(file.Id.ToString());
            return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
        }
    }

    [HttpDelete("safe-delete-folder")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SafeDeleteFolder(string code)
    {
        var folder = folderServe.Get(code);
        if (folder == default) return NotFound(AppLang.Folder_could_not_be_found);

        if (folder.AbsolutePath == "/root" || folder.Type == FolderContentType.SystemFolder)
            return BadRequest(AppLang.Could_not_remove_root_folder);

        if (folder is { Type: FolderContentType.Folder or FolderContentType.HiddenFolder })
        {
            folder.Type = FolderContentType.DeletedFolder;
            var updateResult = await folderServe.UpdateAsync(folder);
            return updateResult.Item1 ? Ok(updateResult.Item2) : BadRequest(updateResult.Item2);
        }

        else
        {
            var updateResult = folderServe.Delete(folder.Id.ToString());
            return updateResult.Item1 ? Ok(updateResult.Item2) : BadRequest(updateResult.Item2);
        }
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

        Dictionary<string, FolderContent> contentsDict = new();
        foreach (var file in currentFolder.Contents) contentsDict.TryAdd(file.Id, file);


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
            contentsDict.Remove(fileId);
            targetFolder.Contents.Add(new FolderContent
            {
                Id = file.Id.ToString(),
                Type = FolderContentType.File
            });
        }

        currentFolder.Contents = contentsDict.Values.ToList();

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
        targetFolder.Contents.Add(new FolderContent
        {
            Id = folderCode,
            Type = FolderContentType.Folder
        });

        await folderServe.UpdateAsync(targetFolder);
        await folderServe.UpdateAsync(folder);
        return Ok(AppLang.Folder_moved_successfully);
    }


    [HttpPost("upload-physical")]
    [DisableFormValueModelBinding]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadPhysical()
    {
        var folderKeyString = AppLang.Folder;
        var fileKeyString = AppLang.File;
        var cancelToken = HttpContext.RequestAborted;


        try
        {
            if (string.IsNullOrEmpty(Request.ContentType) || !MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError(fileKeyString, "The request couldn't be processed (Error 1).");
                return BadRequest(ModelState);
            }

            HttpContext.Request.Headers.TryGetValue("Folder", out var folderValues);

            var folderCodes = folderValues.ToString();

            if (string.IsNullOrEmpty(folderCodes))
            {
                ModelState.AddModelError("Header", "Folder path is required in the request header");
                return BadRequest(ModelState);
            }

            // folderServe.GetRoot(user);
            var folder = folderServe.Get(folderCodes);
            if (folder == null)
            {
                ModelState.AddModelError(folderKeyString, AppLang.Folder_could_not_be_found);
                return BadRequest(ModelState);
            }

            var boundary = MediaTypeHeaderValue.Parse(Request.ContentType).GetBoundary(int.MaxValue);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync(cancelToken);
            while (section != null && HttpContext.RequestAborted is { IsCancellationRequested: false })
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader && contentDisposition != null)
                {
                    if (!contentDisposition.HasFileContentDisposition())
                    {
                        ModelState.AddModelError(fileKeyString, "The request couldn't be processed (Error 2).");
                    }
                    else
                    {
                        var trustedFileNameForDisplay = contentDisposition.FileName.Value ?? Path.GetRandomFileName();

                        var file = new FileInfoModel
                        {
                            FileName = trustedFileNameForDisplay
                        };

                        folder = folderServe.Get(folderCodes);
                        if (folder == null)
                        {
                            if (ModelState.IsValid)
                                return Ok(AppLang.Successfully_uploaded);
                            return BadRequest(ModelState);
                        }

                        var createFileResult = await folderServe.CreateFileAsync(folder, file, cancelToken);
                        if (createFileResult.Item1)
                        {
                            (file.FileSize, file.ContentType) = await section.ProcessStreamedFileAndSave(file.AbsolutePath, ModelState, cancelToken);

                            if (file.FileSize > 0)
                            {
                                var updateResult = await fileServe.UpdateAsync(file, cancelToken);
                                if (updateResult.Item1)
                                {
                                    await thumbnailService.AddThumbnailRequest(file.Id.ToString());
                                }
                                else
                                {
                                    logger.LogError(updateResult.Item2);
                                }
                            }
                            else
                            {
                                logger.LogWarning($"File empty. deleting {file.FileName}");
                                fileServe.Delete(file.Id.ToString());
                            }
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync(cancelToken);
            }

            if (ModelState.IsValid)
                return Ok(AppLang.Successfully_uploaded);

            return Ok(ModelState);
        }
        catch (OperationCanceledException ex)
        {
            return Ok(ex.Message);
        }
        catch (Exception e)
        {
            ModelState.AddModelError(AppLang.Exception, e.Message);
            logger.LogError(e, null);
            return StatusCode(500, ModelState);
        }
    }
}