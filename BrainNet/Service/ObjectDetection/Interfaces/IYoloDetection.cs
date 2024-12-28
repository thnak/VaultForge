using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.ObjectDetection.Interfaces;

public interface IYoloDetection : IDisposable
{
    int GetStride();
    int[] GetInputDimensions();
    public List<YoloBoundingBox> Predict(YoloFeeder feeder);
    public List<YoloBoundingBox> PreprocessAndRun(Image<Rgb24> image);
    public void WarmUp();
    public void SetInput();
    public void SetOutput();
}