using Microsoft.Extensions.VectorData;
using Newtonsoft.Json;

namespace BrainNet.Models.Vector;

public class VectorRecord
{
    [VectorStoreRecordKey] public Guid Index { get; set; } = Guid.NewGuid();

    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public ReadOnlyMemory<float> Vector { get; set; }
}