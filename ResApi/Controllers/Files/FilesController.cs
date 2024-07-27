using System.Net;
using System.Net.Mime;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Utils.Helper;
using BusinessModels.General;
using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace ResApi.Controllers.Files;

[Route("api/[controller]")]
[ApiController]
public class FilesController(IOptions<AppSettings> options, IFileSystemBusinessLayer fileServe, IFolderSystemBusinessLayer folderServe) : ControllerBase
{
    private const long MaxFileSize = 10L * 1024L * 1024L * 1024L; // 10GB, adjust to your need
    private readonly string[] _permittedExtensions = [];
    private readonly string _targetFilePath = options.Value.FileFolder;


    [HttpGet("get-file")]
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
    public IActionResult GetSharedFolder()
    {
        var folder = folderServe.GetRoot("");
        if (folder == null) return BadRequest("Folder not found");
        return Content(folder.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpPost("re-name-file")]
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


    [HttpPost("create-shared-folder")]
    public async Task<IActionResult> CreateSharedFolder([FromBody] RequestNewFolderModel request)
    {
        var folderRoot = folderServe.Get(request.RootId);
        if (folderRoot == null) return NotFound("Can not be found");
        folderServe.Get("", "/");

        var res = await folderServe.CreateAsync(request.NewFolder);
        if (res.Item1)
        {
            folderRoot.Contents.Add(new FolderContent
            {
                Id = request.NewFolder.Id.ToString(),
                Type = FolderContentType.Folder
            });
            res = await folderServe.UpdateAsync(folderRoot);
            if (res.Item1)
                return Ok();
            ModelState.AddModelError("Folder", res.Item2);
        }
        else
        {
            ModelState.AddModelError("Folder", res.Item2);
        }

        return BadRequest(ModelState);
    }

    [HttpPost("upload-physical")]
    [DisableFormValueModelBinding]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadPhysical()
    {
        try
        {
            if (string.IsNullOrEmpty(Request.ContentType) || !MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
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
                ModelState.AddModelError("Folder", "Folder not found");
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
                        ModelState.AddModelError("File", "The request couldn't be processed (Error 2).");
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
            ModelState.AddModelError("Exception", e.Message);
            return StatusCode(500, ModelState);
        }
    }
}