using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;

namespace BrainNet.Service.ObjectDetection.Interfaces;

public interface IYoloDetection : IDisposable
{
    public List<YoloBoundingBox> Predict(YoloFeeder feeder);
}