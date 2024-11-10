namespace BrainNet.Service.ObjectDetection.Model.Result;

public class YoloPrediction
{
    private float[] PredictionArrays { get; set; }
    private string[] Categories { get; set; }
    private List<float[]> Dwdhs { get; set; }
    private List<int[]> ImageShapes { get; set; }
    private List<float[]> Ratios { get; set; }

    /// <summary>
    /// init an object that hold all prediction at one
    /// </summary>
    /// <param name="predictionArrayResults"></param>
    /// <param name="categories"></param>
    /// <param name="dhdws"></param>
    /// <param name="ratios"></param>
    /// <param name="imageShapes"></param>
    public YoloPrediction(float[] predictionArrayResults, string[] categories, List<float[]> dhdws, List<float[]> ratios, List<int[]> imageShapes)
    {
        PredictionArrays = predictionArrayResults;
        Categories = categories;
        Dwdhs = dhdws;
        ImageShapes = imageShapes;
        Ratios = ratios;
    }

    /// <summary>
    /// get all prediction
    /// </summary>
    /// <returns>a list of Yolov7Predict</returns>
    public List<YoloBoundingBox> GetDetect()
    {
        List<YoloBoundingBox> yoloBoundingBoxs = new List<YoloBoundingBox>();

        int length = PredictionArrays.Length;
        if (length > 0)
        {
            length /= 7;
            Parallel.For(0, length, i =>
            {
                int start = i * 7;
                int end = start + 7;
                float[] slice = PredictionArrays[start..end];
                int clsIdx = (int)slice[5];
                int batchId = (int)slice[0];
                float[] boxArray = [slice[1], slice[2], slice[3], slice[4]];
                float[] doubleDwDhs = [Dwdhs[batchId][1], Dwdhs[batchId][0], Dwdhs[batchId][1], Dwdhs[batchId][0]];

                boxArray[0] -= doubleDwDhs[0];
                boxArray[1] -= doubleDwDhs[1];
                boxArray[2] -= doubleDwDhs[2];
                boxArray[3] -= doubleDwDhs[3];
                boxArray = boxArray.Select(x => Math.Max(x / Ratios[batchId][0], 0)).ToArray();

                int[] box = new[] { (int)Math.Round(boxArray[0]), (int)Math.Round(boxArray[1]), (int)Math.Round(boxArray[2]), (int)Math.Round(boxArray[3]) };
                yoloBoundingBoxs.Add(new YoloBoundingBox()
                {
                    BatchId = (int)slice[0],
                    ClassIdx = clsIdx,
                    Score = slice[6],
                    ClassName = Categories[clsIdx],
                    Box = box,
                    Bbox = Xyxy2Xywh(box)
                });
            });
        }

        return yoloBoundingBoxs;
    }

    /// <summary>
    /// convert xyxy to xywh 
    /// </summary>
    /// <param name="inputs"></param>
    /// <returns></returns>
    private static int[] Xyxy2Xywh(IReadOnlyList<int> inputs)
    {
        var feed = new int[inputs.Count];
        feed[0] = inputs[0];
        feed[1] = inputs[1];
        feed[2] = inputs[2] - inputs[0];
        feed[3] = inputs[3] - inputs[1];
        return feed;
    }
}