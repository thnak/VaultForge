namespace BrainNet.Models.Setting;

public class VectorDbConfig
{
    public string Name { get; set; } = string.Empty;
    public string OllamaConnectionString { get; set; } = "http://localhost:11434/";
    public string OllamaTextEmbeddingModelName { get; set; } = "all-minilm";
}