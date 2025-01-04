namespace BrainNet.Service.ObjectDetection.Model.Feeder;

public class YoloInferenceServiceFeeder(float[] buffer)
{
    public int OriginImageHeight { get; set; }
    public int OriginImageWidth { get; set; }

    public float WidthRatio { get; set; }
    public float HeightRatio { get; set; }

    public float PadHeight { get; set; }
    public float PadWidth { get; set; }

    public float[] Buffer { get; set; } = buffer;
}