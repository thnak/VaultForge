using System.Net.Mime;
using System.Text;
using BrainNet.Service.FaceEmbedding.Implements;
using BrainNet.Service.ObjectDetection.Implements;
using BrainNet.Service.ObjectDetection.Model.Feeder;
using Business.Business.Interfaces.User;
using Business.Models.RetrievalAugmentedGeneration.Vector;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ResApi.Controllers.Test;

[ApiController]
[Route("[controller]")]
public class TestController(ILogger<TestController> logger, IFaceBusinessLayer faceBusinessLayer) : ControllerBase
{
    [HttpGet("test")]
    public async Task<IActionResult> Index()
    {
        using var facedetection = new YoloDetection("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/best.onnx");
        using var faceEmbedding = new FaceEmbedding("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/just_reshape.onnx");

        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        string folderPath = "C:/Users/thanh/Downloads/archive/Faces/Faces";

        var imageFiles = new List<string>();
        foreach (var fileImage in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower())))
        {
            imageFiles.Add(fileImage);
        }

        logger.LogInformation($"Found {imageFiles.Count} images");

        var fileGroupByName = imageFiles.GroupBy(image => Path.GetFileNameWithoutExtension(image).Split("_").First());

        foreach (var fileGroup in fileGroupByName)
        {
            foreach (var file in fileGroup)
            {
                var tensorFeed = new YoloFeeder(facedetection.InputDimensions[2..], facedetection.Stride);
                var image = Image.Load<Rgb24>(file);

                tensorFeed.SetTensor(image);
                var array = facedetection.Predict(tensorFeed);

                foreach (var box in array)
                {
                    var outputImage = image.Clone();
                    outputImage.Mutate(i => i.Crop(new Rectangle(box.X, box.Y, box.Width, box.Height)));
                    using MemoryStream memoryStream = new MemoryStream();
                    await outputImage.SaveAsJpegAsync(memoryStream);

                    memoryStream.Seek(0, SeekOrigin.Begin);

                    var vector = faceEmbedding.GetEmbeddingArray(memoryStream);

                    var faceStorage = await faceBusinessLayer.SearchVectorAsync(vector);
                    if (faceStorage.IsSuccess)
                    {
                        var face = faceStorage.Value.First();
                        if (face.Score <= 0.98)
                        {
                            logger.LogInformation($"Found vector {face.Value.Key} for {fileGroup.Key} {face.Score:P1}");
                            await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
                            {
                                Vector = vector,
                                Owner = fileGroup.Key ?? string.Empty,
                            });
                        }
                    }
                    else
                    {
                        logger.LogWarning("Failed to find vector");
                        await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
                        {
                            Vector = vector,
                            Owner = fileGroup.Key ?? string.Empty,
                        });
                    }
                }
            }
        }

        return Ok();
    }

    [HttpGet("test2")]
    public async Task<IActionResult> Index2()
    {
        using var faceEmbedding = new FaceEmbedding("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/arcfaceresnet100-8.onnx");

        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        string folderPath = "C:/Users/thanh/Downloads/archive/Faces/Faces";

        var imageFiles = new List<string>();
        foreach (var fileImage in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories).Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower())))
        {
            imageFiles.Add(fileImage);
        }

        logger.LogInformation($"Found {imageFiles.Count} images");

        var fileGroupByName = imageFiles.GroupBy(image => Path.GetFileNameWithoutExtension(image).Split("_").First());

        foreach (var fileGroup in fileGroupByName)
        {
            foreach (var file in fileGroup)
            {
                var vector = faceEmbedding.GetEmbeddingArray(file);

                var faceStorage = await faceBusinessLayer.SearchVectorAsync(vector);
                if (faceStorage.IsSuccess)
                {
                    var face = faceStorage.Value.First();
                    if (face.Score <= 0.98)
                    {
                        logger.LogInformation($"Found vector {face.Value.Key} for {fileGroup.Key} {face.Score:P1}");
                        await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
                        {
                            Vector = vector,
                            Owner = fileGroup.Key ?? string.Empty,
                        });
                    }
                }
                else
                {
                    logger.LogWarning("Failed to find vector");
                    await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
                    {
                        Vector = vector,
                        Owner = fileGroup.Key ?? string.Empty,
                    });
                }
            }
        }

        return Ok();
    }

    [HttpPost("seach-face")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Search([FromForm] IFormFile file)
    {
        using var faceEmbedding = new FaceEmbedding("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/just_reshape.onnx");
        using var facedetection = new YoloDetection("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/best.onnx");
        var tensorFeed = new YoloFeeder(facedetection.InputDimensions[2..], facedetection.Stride);
        using MemoryStream memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var image = Image.Load<Rgb24>(memoryStream);


        tensorFeed.SetTensor(image);
        var array = facedetection.Predict(tensorFeed);
        StringBuilder stringBuilder = new StringBuilder();

        foreach (var box in array)
        {
            var outputImage = image.Clone();
            outputImage.Mutate(i => i.Crop(new Rectangle(box.X, box.Y, box.Width, box.Height)));
            using MemoryStream dropImageStream = new MemoryStream();
            await outputImage.SaveAsJpegAsync(dropImageStream);
            dropImageStream.Seek(0, SeekOrigin.Begin);

            var vector = faceEmbedding.GetEmbeddingArray(dropImageStream);
            var result = await faceBusinessLayer.SearchVectorAsync(vector);
            if (result.IsSuccess)
            {
                foreach (var score in result.Value)
                {
                    stringBuilder.AppendLine($"{score.Value.Key}: {score.Score:P1}");
                }
            }
        }

        string responseText = stringBuilder.ToString();
        return Content(responseText, MediaTypeNames.Text.RichText);
    }
}