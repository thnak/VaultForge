using System.Net;
using System.Net.Mime;
using Business.Attribute;
using Business.Business.Interfaces.FileSystem;
using Business.Utils.Helper;
using BusinessModels.General;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
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

    [HttpPost("upload-physical")]
    [DisableFormValueModelBinding]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadPhysical()
    {
        List<string> files = [];
        if (string.IsNullOrEmpty(Request.ContentType) || !MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
        {
            ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
            return BadRequest(ModelState);
        }

        HttpContext.Request.Headers.TryGetValue("Folder", out StringValues folderValues);
        
        var user = HttpContext.User.Identity?.Name ?? string.Empty;
        var folderCodes = folderValues.ToString();
        
        if (string.IsNullOrEmpty(folderCodes))
        {
            ModelState.AddModelError("Header", "Folder path is required in the request header");
            return BadRequest(ModelState);
        }

        folderServe.GetRoot(user);
        var folder = folderServe.Get(user, folderCodes);
        

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
                    var b = section.Body;
                }
            }

            section = await reader.ReadNextSectionAsync();
        }

        return Ok(ModelState);
    }
}