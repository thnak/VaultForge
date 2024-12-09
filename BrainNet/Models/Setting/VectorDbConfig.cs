using Microsoft.Extensions.VectorData;

namespace BrainNet.Models.Setting;

public class VectorDbConfig
{
    public string Name { get; set; } = string.Empty;
    public int VectorSize { get; set; } = 512;
    public string IndexKind { get; set; } = "Dynamic";
    public string DistantFunc { get; set; } = DistanceFunction.CosineSimilarity;
    public string OllamaConnectionString { get; set; } = "http://localhost:11434/";
    public string OllamaTextEmbeddingModelName { get; set; } = "all-minilm";
    public string OllamaImage2TextModelName { get; set; } = "all-minilm";
}