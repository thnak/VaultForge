namespace BrainNet.Models.Setting;

public class BrainNetSettingModel
{
    public FaceEmbeddingSettingModel FaceEmbeddingSetting { get; set; } = new();
}

public class FaceEmbeddingSettingModel
{
    public string FaceEmbeddingPath { get; set; } = string.Empty;
    public int DeviceIndex { get; set; }
    public int Height { get; set; } = 244;
    public int Width { get; set; } = 244;
}