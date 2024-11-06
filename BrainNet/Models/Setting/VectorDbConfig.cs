namespace BrainNet.Models.Setting;

public class VectorDbConfig
{
    public string Name { get; set; } = string.Empty;
    public string OllamaConnectionString { get; set; } = "http://localhost:11434/";
    public string OllamaTextEmbeddingModelName { get; set; } = "all-minilm";
    public string OllamaImage2TextModelName { get; set; } = "all-minilm";
    public double SearchThresholds { get; set; } = 0.5;
}