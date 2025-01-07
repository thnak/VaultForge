using BrainNet.Service.ObjectDetection;
using Microsoft.AspNetCore.Mvc;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using FontFamily = BrainNet.Service.Font.Model.FontFamily;

namespace WebApp.Controllers.Inference;

public partial class YoloInferenceController
{
    [HttpPost("request-key")]
    public IActionResult GetKey([FromForm] IFormFile file)
    {
        var key = yoloSessionManager.RegisterService(file.OpenReadStream());
        return Ok(key.ToString());
    }

    [HttpPost("predict")]
    [RequestSizeLimit(524288000 * 2)] // 500 MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000 * 2, ValueLengthLimit = 524288000 * 2)]
    public async Task<IActionResult> Pred([FromForm] IFormFile file, [FromForm] string api)
    {
        Guid guid = Guid.Parse(api);
        var image = await Image.LoadAsync<Rgb24>(file.OpenReadStream());

        Response.RegisterForDispose(image);

        if (yoloSessionManager.TryGetService(guid, out var service))
        {
            var predictResult = await service!.AddInputAsync(image);
            if (predictResult.IsSuccess)
            {
                var font = fontServiceProvider.CreateFont(FontFamily.RobotoRegular, 12, FontStyle.Regular);
                var resultImage = image.PlotImage(font, predictResult.Value);
                Response.RegisterForDispose(resultImage);

                MemoryStream ms = new();
                Response.RegisterForDispose(ms);

                await resultImage.SaveAsPngAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                return new FileStreamResult(ms, "image/png");
            }
        }

        return BadRequest("Not found service by key");
    }
}