using BrainNet.Service.ObjectDetection.Implements;
using BrainNet.Service.ObjectDetection.Model.Feeder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var imagePath = "C:/Users/thanh/OneDrive/Pictures/WallPaper/soccer-1401929.jpg";

using var embedding = new YoloDetection("C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/best.onnx");
var tensorFeed = new YoloFeeder([640, 640], 32);
tensorFeed.SetTensor(imagePath);
var array =  embedding.Predict(tensorFeed);
// var prediction = new YoloPrediction(array, "face", )

var image = Image.Load<Rgb24>(imagePath);

int index = 0;
foreach (var box in array)
{
    var outputImage = image.Clone();
    outputImage.Mutate(i=>i.Crop(new Rectangle(box.X, box.Y, box.Width, box.Height)));
    outputImage.SaveAsJpeg($"C:/Users/thanh/Git/CodeWithMe/ConsoleApp1/face_{index}.jpg");
    index += 1;
}
Console.WriteLine(array);