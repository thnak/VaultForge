namespace BrainNet.Models.Setting;

public class BrainNetSettingModel
{
    public FaceEmbeddingSettingModel FaceEmbeddingSetting { get; set; } = new();
    public DetectionSettingModel DetectionSetting { get; set; } = new();
}

public class FaceEmbeddingSettingModel
{
    public string FaceEmbeddingPath { get; set; } = string.Empty;
    public int DeviceIndex { get; set; }
}

public class DetectionSettingModel
{
    public string DetectionPath { get; set; } = string.Empty;
}