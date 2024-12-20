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

    public List<int> PredictWaterMeter(YoloFeeder feeder)
    {
        var pred = _yoloDetection.Predict(feeder).OrderBy(x => x.BatchId).ThenBy(x => x.X).GroupBy(x => x.BatchId).ToList().Select(x => x.Select(box => box.ClassIdx).ToList());
        List<int> result = new();
        foreach (var box in pred)
        {
            var valueText = string.Join("", box);
            int.TryParse(valueText, out int value);
            result.Add(value);
        }

        return result;
    }

    public void Dispose()
    {
        _yoloDetection.Dispose();
    }
}