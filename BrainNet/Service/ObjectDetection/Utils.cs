using BrainNet.Service.ObjectDetection.Model.Result;

namespace BrainNet.Service.ObjectDetection;

public static class Utils
{
    public static int Area(this YoloBoundingBox boundingBox)
    {
        return boundingBox.Height * boundingBox.Width;
    }
}