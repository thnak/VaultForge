using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;

namespace BrainNet.Service.WaterMeter.Implements;

public interface IWaterMeterReader : IDisposable
{
    int GetStride();
    int[] GetInputDimensions();
    public List<YoloBoundingBox> Predict(YoloFeeder feeder);
    int PredictWaterMeter(YoloFeeder feeder);
}