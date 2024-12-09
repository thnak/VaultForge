using Microsoft.Extensions.VectorData;

namespace BrainNet.Models.Vector;

public class VectorRecord
{
    [VectorStoreRecordKey] public Guid Index { get; set; } = Guid.NewGuid();

    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ReadOnlyMemory<float> Vector { get; set; }
}