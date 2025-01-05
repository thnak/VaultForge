using BrainNet.Models.Vector;

namespace BrainNet.Service.ObjectDetection.Model.Feeder;

public record YoloInferenceServiceFeeder(MemoryTensor<float> Buffer)
{
    public int OriginImageHeight { get; set; }
    public int OriginImageWidth { get; set; }

    public float WidthRatio { get; set; }
    public float HeightRatio { get; set; }

    public float PadHeight { get; set; }
    public float PadWidth { get; set; }

    public MemoryTensor<float> Buffer { get; set; } = Buffer;
}