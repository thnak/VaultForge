using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public class FileController : ControllerBase
{
    [HttpGet("File")]
    public IActionResult Index()
    {
        return Ok();
    }
    [HttpPost("testfdsjhfids")]
    public IActionResult TestEndpoint(string name)
    {
        return Ok(new
        {
            message = $"Hello, {name}!"
        });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadChunk([FromForm] FormChunk form)
    {

        var (chunk, chunkIndexString, totalChunksString, fileName) = (form.Chunk, form.ChunkIndexString, form.TotalChunksString, form.FileName);
        var chunkIndex = int.Parse(chunkIndexString);
        var totalChunks = int.Parse(totalChunksString);

        if (chunk == null || chunk.Length == 0)
        {
            return BadRequest("No chunk uploaded");
        }

        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, $"{fileName}.part{chunkIndex}");

        // Save chunk to disk
        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
        {
            await chunk.CopyToAsync(stream);
        }

        if (chunkIndex == totalChunks - 1)
        {
            // Combine chunks into the final file
            var finalFilePath = Path.Combine(uploadPath, fileName);

            using (var finalStream = new FileStream(finalFilePath, FileMode.Append))
            {
                for (var i = 0; i < totalChunks; i++)
                {
                    var partPath = Path.Combine(uploadPath, $"{fileName}.part{i}");
                    using (var partStream = new FileStream(partPath, FileMode.Open))
                    {
                        await partStream.CopyToAsync(finalStream);
                    }
                    System.IO.File.Delete(partPath);// Delete part file after merging
                }
            }

            return Ok(new
            {
                filePath = finalFilePath
            });
        }

        return Ok();
    }

    public class FormChunk
    {
        public IFormFile Chunk { get; set; }
        public string ChunkIndexString { get; set; } = string.Empty;
        public string TotalChunksString { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}