using System.Net;
using System.Net.Mime;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Utils.Helper;
using BusinessModels.General;
using BusinessModels.General.EnumModel;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using BusinessModels.WebContent;
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
            FileName = file.FileName.Replace(".bin", file.ContentType.GetCorrectExtensionFormContentType()),
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
            FileName = file.FileName.Replace(".bin", file.ContentType.GetCorrectExtensionFormContentType()),
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
        foreach (var id in listFiles)
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
        foreach (var id in listFolders)
        {
            var file = folderServe.Get(id);
            if (file == null) continue;
            files.Add(file);
        }

        return Content(files.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("get-file-v2")]
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

    [HttpGet("get-shared-folder")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [OutputCache(Duration = 3600, NoStore = true)]
    public IActionResult GetSharedFolder(string? id)
    {
        var folder = string.IsNullOrEmpty(id) ? folderServe.GetRoot("") : folderServe.Get(id);
        if (folder == null) return BadRequest(AppLang.Folder_could_not_be_found);
        folder.Password = string.Empty;
        return Content(folder.ToJson(), MediaTypeNames.Application.Json);
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
    public async Task<IActionResult> SafeDeleteFile([FromForm] string code)
    {
        var file = fileServe.Get(code);
        if (file == default) return NotFound(AppLang.File_not_found_);

        file.Type = FileContentType.DeletedFile;
        var result = await fileServe.UpdateAsync(file);
        return result.Item1 ? Ok(result.Item2) : BadRequest(result.Item2);
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
                ModelState.AddModelError("File", AppLang.File_not_found_);
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
                ModelState.AddModelError("File", x.Item2);


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
            while (section != null)
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
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(contentDisposition.FileName.Value) ?? Path.GetRandomFileName();

                        var file = new FileInfoModel
                        {
                            FileName = trustedFileNameForDisplay
                        };
                        folderServe.CreateFile(folder, file);
                        (file.FileSize, file.ContentType) = await section.ProcessStreamedFileAndSave(file.AbsolutePath, ModelState);
                        await fileServe.UpdateAsync(file);
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            return Ok(ModelState);
        }
        catch (Exception e)
        {
            ModelState.AddModelError(AppLang.Exception, e.Message);
            return StatusCode(500, ModelState);
        }
    }
}