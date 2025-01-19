using System.Net.Mime;
using BrainNet.Models.Result;
using BrainNet.Models.Vector;
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
    public async Task<IActionResult> InsertFace([FromForm] List<IFormFile> files, [FromForm] string owner)
    {
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync<Rgb24>(stream);
            var vector = await faceEmbeddingInferenceService.AddInputAsync(image);
            await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel
            {
                CreatedAt = DateTime.Now,
                Owner = owner
            }, vector);
        }


        return Content("ok", MediaTypeNames.Application.Json);
    }

    [HttpPost("search-face")]
    public async Task<IActionResult> SearchFace([FromForm] IFormFile file, [FromForm] int limit, [FromForm] double alpha, [FromForm] double beta, [FromForm] double threshold)
    {
        await using var stream = file.OpenReadStream();
        using var image = await Image.LoadAsync<Rgb24>(stream);
        var vector = await faceEmbeddingInferenceService.AddInputAsync(image);

        var searchResults = await faceBusinessLayer.SearchVectorAsync(vector, limit);

        var scorer = new SearchScorer<VectorRecord>();
        var classScores = scorer.GetWeightedTopScores<string>(
            searchResults.Value ?? [],
            value => value.Key, // Class based on the first letter (A or B)
            alpha, // Weight for sum of scores
            threshold // Weight for density
        );

        return Content(classScores.ToJson(), MediaTypeNames.Application.Json);
    }
}