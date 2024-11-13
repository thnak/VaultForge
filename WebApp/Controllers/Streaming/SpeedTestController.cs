using System.Diagnostics;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.Streaming;

[Route("api/[controller]")]
[ApiController]
public class SpeedTestController : ControllerBase
{
    private const long MemoryStreamSize = 1024 * 1024 * 1024;
    [HttpGet("stream")]
    public async Task<IActionResult> StreamVideo()
    {
        
        var stopwatch = new Stopwatch();

        try
        {
            // Create a MemoryStream with a defined size (e.g., 10 MB)
            byte[] data = new byte[MemoryStreamSize];
            var memoryStream = new MemoryStream(data);

            // Start the stopwatch before sending the data
            stopwatch.Start();

            // Send the memory stream to the client
            await memoryStream.CopyToAsync(Response.Body);

            // Stop the stopwatch after the data is sent
            stopwatch.Stop();

            // Calculate the download speed in Mbps
            double downloadTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
            double downloadSpeedMbps = MemoryStreamSize * 8 / (downloadTimeInSeconds * 1024 * 1024); // bits per second to Mbps
        
            Response.RegisterForDisposeAsync(memoryStream);

            
            // Return the result as a JSON object

            var result = new
            {
                DownloadSpeedMbps = downloadSpeedMbps,
                DownloadTimeSeconds = downloadTimeInSeconds
            };
            
            return Content(result.ToJson(), MimeTypeNames.Application.Json);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error occurred: {ex.Message}");
        }
    }
}