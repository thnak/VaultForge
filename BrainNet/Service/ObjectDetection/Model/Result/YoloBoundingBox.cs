
namespace BrainNet.Service.ObjectDetection.Model.Result;

public class YoloBoundingBox
{
    /// <summary>
    /// index of batch
    /// </summary>
    public int BatchId { get; set; }

    /// <summary>
    /// bounding box with shape x y width height
    /// </summary>
    public int[] Bbox { get; init; } = [];

    /// <summary>
    /// box with shape x0 y0 x1 y1
    /// </summary>
    public int[] Box { get; init; } = [];

    /// <summary>
    /// category index
    /// </summary>
    public int ClassIdx { get; init; }

    /// <summary>
    /// category name
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// confident score
    /// </summary>
    public float Score { get; init; }

    public int X => Bbox[0];
    public int Y => Bbox[1];
    public int Height => Bbox[3];
    public int Width => Bbox[2];
}