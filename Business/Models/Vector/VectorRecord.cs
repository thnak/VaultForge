using BusinessModels.Base;

namespace Business.Models.Vector;

public class VectorRecord : BaseModelEntry
{
    public string Collection  { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public float[] Vector { get; set; } = [];
}