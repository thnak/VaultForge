using System.Net.Mime;
using Business.Business.Interfaces.User;
using Business.Models.RetrievalAugmentedGeneration.Vector;
using Business.Services.OnnxService.Face;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WebApp.Controllers.Inference;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/face")]
[ApiController]
public class FaceController(IFaceEmbeddingInferenceService faceEmbeddingInferenceService, IFaceBusinessLayer faceBusinessLayer) : ControllerBase
{
    [HttpPost("insert-new-face")]
    public async Task<IActionResult> InsertFace([FromForm] IFormFile file, string owner)
    {
        await using var stream = file.OpenReadStream();
        var image = await Image.LoadAsync<Rgb24>(stream);
        var vector = await faceEmbeddingInferenceService.AddInputAsync(image);
        var updateResult = await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
        {
            CreatedAt = DateTime.Now,
            Owner = owner,
        }, vector);

        return Content(updateResult.ToJson(), MediaTypeNames.Application.Json);
    }
}