using System.Net.Mime;
using Business.Models;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.Streaming;

[Route("api/[controller]")]
[ApiController]
public class VideoController : ControllerBase
{
    [HttpGet("stream/{fileName}")]
    public IActionResult StreamVideo(string fileName)
    {
        fileName = fileName.DecodeBase64String();
        var filePath = Path.Combine("C:/Users/thanh/Downloads", fileName);
        if (!global::System.IO.File.Exists(filePath)) return NotFound();
        var cd = new ContentDisposition
        {
            FileName = fileName,
            Inline = true // false = prompt the user for downloading;  true = browser to try to show the file inline
        };
        const int bufferSize = 4 * 1024 * 1024;

        if (!FileSignatureValidator.ValidateFileSignature(filePath, FileSignatureValidator.Mp4Signature)) return BadRequest();

        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous);

        Response.RegisterForDisposeAsync(fileStream);
        Response.Headers.Append("Content-Disposition", cd.ToString());

        return File(fileStream, "video/mp4", fileName, true);
    }
}