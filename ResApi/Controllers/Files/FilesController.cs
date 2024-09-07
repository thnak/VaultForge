using System.Net.Mime;
using System.Web;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Utils.Helper;
using BusinessModels.General;
using BusinessModels.General.EnumModel;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using BusinessModels.WebContent.Drive;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Protector.Utils;

namespace ResApi.Controllers.Files;

[Route("api/[controller]")]
[ApiController]
public class FilesController(IOptions<AppSettings> options, IFileSystemBusinessLayer fileServe, IFolderSystemBusinessLayer folderServe) : ControllerBase
{
    [HttpGet("get-file")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetFile(string id)
    {
        var file = fileServe.Get(id);
        if (file == null) return NotFound();
        var now = DateTime.UtcNow;
        var cd = new ContentDisposition
        {
            FileName = HttpUtility.UrlEncode(file.FileName.Replace(".bin", file.ContentType.GetCorrectExtensionFormContentType())),
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
            FileName = HttpUtility.UrlEncode(file.FileName.Replace(".bin", file.ContentType.GetCorrectExtensionFormContentType())),
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
            if (file is { Type: FileContentType.DeletedFile or FileContentType.HiddenFile or FileContentType.ThumbnailFile })
                continue;
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
        foreach (var id in listFolders.TakeWhile(_ => cancelToken is not { IsCancellationRequested: true }))
        {
            var file = folderServe.Get(id);
            if (file == null) continue;
            files.Add(file);
        }

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
    [OutputCache(Duration = 5, NoStore = true)]
    public IActionResult GetSharedFolder([FromForm] string? id, [FromForm] string? password,
        [FromForm] int page, [FromForm] int pageSize, [FromForm] string? contentTypes)
    {
        var folder = string.IsNullOrEmpty(id) ? folderServe.GetRoot("") : folderServe.Get(id);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);
        if (!string.IsNullOrEmpty(folder.Password))
        {
            if (password == null || password.ComputeSha256Hash() != folder.Password)
                return Unauthorized(AppLang.This_resource_is_protected_by_password);
        }

        folder.Password = string.Empty;

        List<FolderContentType> contentTypesList;

        if (!string.IsNullOrEmpty(contentTypes))
        {
            var listString = contentTypes.DeSerialize<List<string>>();
            if (listString != null) contentTypesList = listString.Select(Enum.Parse<FolderContentType>).ToList();
            else
            {
                contentTypesList = contentTypes.DeSerialize<List<FolderContentType>>() ?? [];
            }
        }
        else
        {
            contentTypesList = Enum.GetValues<FolderContentType>().ToList();
        }

        page -= 1;

        folder.Contents = folder.Contents.Where(x => contentTypesList.Contains(x.Type)).ToList();
        var size = (int)Math.Ceiling(folder.Contents.Count / (float)pageSize);
        folder.Contents = folder.Contents.Skip(page * pageSize).Take(pageSize).ToList();
        FolderRequest folderRequest = new FolderRequest()
        {
            Folder = folder,
            TotalPages = size
        };
        return Content(folderRequest.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpPost("search-folder")]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 10, NoStore = true)]
    public async Task<IActionResult> SearchFolder([FromForm] string? username, [FromForm] string searchString)
    {
        List<FolderInfoModel> folderList = [];
        var cancelToken = HttpContext.RequestAborted;

        var user = folderServe.GetUser(string.Empty) ?? new UserModel();

        try
        {
            await foreach (var x in folderServe.Where(x => x.FolderName.Contains(searchString) ||
                                                           x.RelativePath.Contains(searchString) &&
                                                           x.Username == user.UserName &&
                                                           x.Type == FolderContentType.Folder, cancelToken).WithCancellation(cancelToken))
            {
                x.Password = string.Empty;
                x.SharedTo = [];
                x.Contents = [];
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
        var userName = User.Identity?.Name ?? string.Empty;
        var folders = folderServe.GetFolderBloodLine(userName, id);
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
        folder.FolderName = newName;
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
        var file = fileServe.Get(code);
        if (file == default) return NotFound(AppLang.File_not_found_);
        if (file is { Type: FileContentType.File or FileContentType.HiddenFile })
        {
            file.Type = FileContentType.DeletedFile;
            var result = await fileServe.UpdateAsync(file);
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

        if (folder.RelativePath == "/root")
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

        await folderServe.UpdateAsync(targetFolder);
        await folderServe.UpdateAsync(currentFolder);
        await foreach (var x in fileServe.UpdateAsync(files!))
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
        try
        {
            if (string.IsNullOrEmpty(Request.ContentType) || !MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError(fileKeyString, "The request couldn't be processed (Error 1).");
                return BadRequest(ModelState);
            }

            HttpContext.Request.Headers.TryGetValue("Folder", out var folderValues);

            var user = HttpContext.User.Identity?.Name ?? string.Empty;
            var folderCodes = folderValues.ToString();

            if (string.IsNullOrEmpty(folderCodes))
            {
                ModelState.AddModelError("Header", "Folder path is required in the request header");
                return BadRequest(ModelState);
            }

            folderServe.GetRoot(user);
            var folder = folderServe.Get(user, folderCodes);
            if (folder == null)
            {
                ModelState.AddModelError(folderKeyString, AppLang.Folder_could_not_be_found);
                return BadRequest(ModelState);
            }

            var boundary = MediaTypeHeaderValue.Parse(Request.ContentType).GetBoundary(int.MaxValue);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
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
                        folder = folderServe.Get(user, folderCodes);
                        if (folder == null)
                        {
                            ModelState.AddModelError(folderKeyString, AppLang.Folder_could_not_be_found);
                            return BadRequest(ModelState);
                        }

                        folderServe.CreateFile(folder, file);
                        (file.FileSize, file.ContentType) = await section.ProcessStreamedFileAndSave(file.AbsolutePath, ModelState, HttpContext.RequestAborted);
                        if (file.FileSize > 0)
                            await fileServe.UpdateAsync(file);
                        else
                        {
                            fileServe.Delete(file.Id.ToString());
                            folder = folderServe.Get(user, folderCodes)!;
                            folder.Contents = folder.Contents.Where(x => x.Id != file.Id.ToString()).ToList();
                            await folderServe.UpdateAsync(folder);
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync();
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
            return StatusCode(500, ModelState);
        }
    }
}