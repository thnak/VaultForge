namespace BrainNet.Models.Setting;

public class BrainNetSettingModel
{
    public FaceEmbeddingSettingModel FaceEmbeddingSetting { get; set; } = new();
    public DetectionSettingModel WaterSetting { get; set; } = new();
}

public class FaceEmbeddingSettingModel
{
    public string FaceEmbeddingPath { get; set; } = string.Empty;
    public int DeviceIndex { get; set; }
    public int PeriodicTimer { get; set; } = 10;
    public int MaxQueSize { get; set; } = 1000;
    public int IndexVectorSize { get; set; } = 4096;
}

public class DetectionSettingModel
{
    public string DetectionPath { get; set; } = string.Empty;
    public int DeviceIndex { get; set; }
    public int PeriodicTimer { get; set; } = 10;
    public int MaxQueSize { get; set; } = 1000;
}