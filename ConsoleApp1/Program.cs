using BrainNet.Database;
using BrainNet.Models.Setting;
using BrainNet.Models.Vector;
using BrainNet.Service.FaceEmbedding.Implements;
using BrainNet.Service.ObjectDetection.Implements;
using BrainNet.Service.ObjectDetection.Model.Feeder;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


using var facedetection = new YoloDetection("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/best.onnx");
using var faceEmbedding = new FaceEmbedding("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/just_reshape.onnx");



#pragma warning disable SKEXP0020
IVectorDb vectorDb = new VectorDb(new VectorDbConfig()
#pragma warning restore SKEXP0020
{
    Name = "face",
    SearchThresholds = 0.5
}, NullLogger.Instance);

string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
string folderPath = "C:/Users/thanh/Downloads/archive";
// Get all files in the folder with allowed extensions
var imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly).Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower()));

foreach (var file in imageFiles)
{
    Console.WriteLine($"Processing image {file}...");
    var tensorFeed = new YoloFeeder(facedetection.InputDimensions[2..], facedetection.Stride);
    var image = Image.Load<Rgb24>(file);

    tensorFeed.SetTensor(image);
    var array = facedetection.Predict(tensorFeed);

    foreach (var box in array)
    {
        var outputImage = image.Clone();
        outputImage.Mutate(i => i.Crop(new Rectangle(box.X, box.Y, box.Width, box.Height)));
        using MemoryStream memoryStream = new MemoryStream();
        outputImage.SaveAsJpeg(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        var vector = faceEmbedding.GetEmbeddingArray(memoryStream);

        var faceStorage = vectorDb.Search(vector, 10, default);
        await foreach (var storage in faceStorage)
        {
            if (storage.Score > 0.5)
            {
                await vectorDb.AddNewRecordAsync(new VectorRecord()
                {
                    Vector = vector,
                    Key = file
                });
                Console.WriteLine("Same face");
            }
            else
            {
                await vectorDb.AddNewRecordAsync(new VectorRecord()
                {
                    Vector = vector,
                    Key = storage.Value.Key
                });
            }
        }
    }
}