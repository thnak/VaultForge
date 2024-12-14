using System.ComponentModel.DataAnnotations;
using BusinessModels.Base;

namespace BusinessModels.System.ComputeVision;

public class YoloLabel : BaseModelEntry
{
    [Required] public string FileId { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Label must be a non-negative integer.")]
    public int Label { get; set; }

    [Range(0.0, 1.0, ErrorMessage = "X coordinate must be between 0 and 1.")]
    public float X { get; set; }

    [Range(0.0, 1.0, ErrorMessage = "X coordinate must be between 0 and 1.")]
    public float Y { get; set; }

    [Range(0.0, 1.0, ErrorMessage = "X coordinate must be between 0 and 1.")]
    public float Width { get; set; }

    [Range(0.0, 1.0, ErrorMessage = "X coordinate must be between 0 and 1.")]
    public float Height { get; set; }

    public override string ToString()
    {
        return $"{Label} {X} {Y} {Width} {Height}";
    }
}