using BrainNet.Service.ObjectDetection.Implements;
using BrainNet.Service.ObjectDetection.Interfaces;
using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;
using BrainNet.Service.WaterMeter.Implements;

namespace BrainNet.Service.WaterMeter.Interfaces;

public class WaterMeterReader(string waterMeterWeightPath) : IWaterMeterReader
{
    private readonly IYoloDetection _yoloDetection = new YoloDetection(waterMeterWeightPath);

    public int GetStride()
    {
        return _yoloDetection.GetStride();
    }

    public int[] GetInputDimensions() => _yoloDetection.GetInputDimensions();


    public List<YoloBoundingBox> Predict(YoloFeeder feeder)
    {
        return _yoloDetection.Predict(feeder);
    }

    public int PredictWaterMeter(YoloFeeder feeder)
    {
        var pred = _yoloDetection.Predict(feeder).OrderBy(x => x.X).Select(x => x.ClassIdx);
        var valueText = string.Join("", pred);
        int.TryParse(valueText, out int value);
        return value;
    }

    public void Dispose()
    {
        _yoloDetection.Dispose();
    }
}