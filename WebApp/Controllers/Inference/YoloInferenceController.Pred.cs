using BrainNet.Service.ObjectDetection;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WebApp.Controllers.Inference;

public partial class YoloInferenceController
{
    [HttpPost("request-key")]
    public IActionResult GetKey([FromForm] IFormFile file)
    {
        var key = yoloSessionManager.RegisterService(file.OpenReadStream());
        return Ok(key.ToString());
    }

    // [HttpPost("predict")]
    // public async Task<IActionResult> Pred([FromForm] IFormFile file, [FromForm] string api)
    // {
    //     Guid guid = Guid.Parse(api);
    //     using var image = await Image.LoadAsync<Rgb24>(file.OpenReadStream());
    //     if (yoloSessionManager.TryGetService(guid, out var service))
    //     {
    //         var predictResult = await service!.AddInputAsync(image);
    //         if (predictResult.IsSuccess)
    //         {
    //             // image.PlotImage(service.fo)
    //         }
    //     }
    // }
}