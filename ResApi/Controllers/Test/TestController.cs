﻿using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using BrainNet.Service.FaceEmbedding.Implements;
using BrainNet.Service.ObjectDetection.Implements;
using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Utils;
using Business.Business.Interfaces.User;
using Business.Models.RetrievalAugmentedGeneration.Vector;
using Business.Utils.Enumerable;
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
                                Owner = fileGroup.Key ?? string.Empty,
                            }, vector);
                        }
                    }
                    else
                    {
                        logger.LogWarning("Failed to find vector");
                        await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
                        {
                            Owner = fileGroup.Key ?? string.Empty,
                        }, vector);
                    }
                }
            }
        }

        return Ok();
    }

    [HttpGet("test3")]
    public IActionResult Index3()
    {
        Stopwatch sw = Stopwatch.StartNew();
        GenerateLargeData(int.MaxValue).Mean();
        sw.Stop();
        Console.WriteLine($"Mean: {sw.ElapsedMilliseconds} ms");
        sw.Reset();
        sw.Restart();
        var total = GenerateLargeData(int.MaxValue).Sum();
        sw.Stop();
        Console.WriteLine($"Total: {sw.ElapsedMilliseconds} ms");
        return Ok();
    }

    static IEnumerable<float> GenerateLargeData(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return i * 0.1f;
        }
    }

    [HttpGet("test2")]
    public async Task<IActionResult> Index2()
    {
        using var faceEmbedding = new FaceEmbedding("C:/Users/thanh/Downloads/VGGFace.onnx");

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
                    if (faceStorage.Value.Any())
                    {
                        var face = faceStorage.Value.First();
                        if (face.Score == 0)
                            continue;

                        var isMatch = faceStorage.Value.IsMatchingMostSearchedValue(fileGroup.Key!);
                        if (isMatch)
                            logger.LogInformation($"Found vector {face.Value.Key} for {fileGroup.Key} {face.Score:P1}");
                        await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
                        {
                            Owner = fileGroup.Key ?? string.Empty,
                        }, vector);
                    }
                    else
                    {
                        logger.LogWarning("Failed to find vector");
                        await faceBusinessLayer.CreateAsync(new FaceVectorStorageModel()
                        {
                            Owner = fileGroup.Key ?? string.Empty,
                        }, vector);
                    }
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
        using var faceEmbedding = new FaceEmbedding("C:/Users/thanh/source/VGGFace.onnx");
        using var facedetection = new YoloDetection("C:/Users/thanh/source/best.onnx");
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
                var v = result.Value.Select(x => x).ToList();
                var key = v.GetMostFrequentlySearchedKey();
                stringBuilder.AppendLine($"Found vector {key} in box {box.X}x{box.Y}");
            }
        }

        string responseText = stringBuilder.ToString();
        return Content(responseText, MediaTypeNames.Text.RichText);
    }
}