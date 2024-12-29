using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.WaterMeter.Implements;

public interface IWaterMeterReader : IDisposable
{
    int GetStride();
    int[] GetInputDimensions();
    public List<YoloBoundingBox> Predict(YoloFeeder feeder);
    List<int> PredictWaterMeter(YoloFeeder feeder);
    int PredictWaterMeter(Image<Rgb24> image);
}