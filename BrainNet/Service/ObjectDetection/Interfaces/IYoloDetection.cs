using BrainNet.Service.ObjectDetection.Model.Feeder;
using BrainNet.Service.ObjectDetection.Model.Result;

namespace BrainNet.Service.ObjectDetection.Interfaces;

public interface IYoloDetection : IDisposable
{
    int GetStride();
    int[] GetInputDimensions();
    public List<YoloBoundingBox> Predict(YoloFeeder feeder);
    public void WarmUp();
    public void SetInput();
    public void SetOutput();
}