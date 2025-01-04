using System.Collections.Concurrent;

namespace BrainNet.Service.ObjectDetection.Model.Result;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class YoloPrediction
{
    private float[] PredictionArrays { get; }
    private string[] Categories { get; }
    private List<float[]> PadHeightAndWidths { get; }
    private List<int[]> ImageShapes { get; }
    private List<float[]> Ratios { get; }
    private ArrayPool<float> ArrayPool { get; } = ArrayPool<float>.Shared;
    private int BufferLength { get; }
    public YoloPrediction(float[] predictionArrayResults, int bufferLength, string[] categories, List<float[]> padHeightAndWidths, List<float[]> ratios, List<int[]> imageShapes)
    {
        PredictionArrays = predictionArrayResults;
        Categories = categories;
        PadHeightAndWidths = padHeightAndWidths;
        ImageShapes = imageShapes;
        Ratios = ratios;
        BufferLength = bufferLength;
    }

    public YoloPrediction(ReadOnlySpan<float> predictionArrayResults, int bufferLength, string[] categories, List<float[]> padHeightAndWidths, List<float[]> ratios, List<int[]> imageShapes)
    {
        PredictionArrays = predictionArrayResults.ToArray();
        Categories = categories;
        PadHeightAndWidths = padHeightAndWidths;
        ImageShapes = imageShapes;
        Ratios = ratios;
        BufferLength = bufferLength;
    }

    public List<YoloBoundingBox> GetDetect()
    {
        int length = BufferLength / 7;
        if (length == 0) return new List<YoloBoundingBox>();

        var yoloBoundingBoxes = new ConcurrentBag<YoloBoundingBox>();

        Parallel.For(0, length, i =>
        {
            var slice = PredictionArrays.AsSpan(i * 7, 7);
            var clsIdx = (int)slice[5];
            var batchId = (int)slice[0];

            var boxArray = ArrayPool.Rent(4);
            var adjustedBoxArray = ArrayPool.Rent(4);
            try
            {
                // Extract box coordinates
                slice.Slice(1, 4).CopyTo(boxArray);

                // Adjust box using padding
                if(batchId >= PadHeightAndWidths.Count)
                    return;
                
                var pad = PadHeightAndWidths[batchId];
                var ratios = Ratios[batchId];
                var oriShapes = ImageShapes[batchId];
                
                for (int j = 0; j < 4; j++)
                {
                    boxArray[j] -= (j % 2 == 0 ? pad[1] : pad[0]);
                    adjustedBoxArray[j] = Math.Max(boxArray[j] / ratios[0], 0);
                }

                var box = new[]
                {
                    (int)Math.Round(adjustedBoxArray[0]),
                    (int)Math.Round(adjustedBoxArray[1]),
                    (int)Math.Round(adjustedBoxArray[2]),
                    (int)Math.Round(adjustedBoxArray[3])
                };

                lock (yoloBoundingBoxes) // To prevent concurrent list modification
                {
                    yoloBoundingBoxes.Add(new YoloBoundingBox
                    {
                        BatchId = batchId,
                        ClassIdx = clsIdx,
                        Score = slice[6],
                        ClassName = Categories[clsIdx],
                        Box = box,
                        Bbox = Xyxy2Xywh(box, oriShapes[0], oriShapes[1])
                    });
                }
            }
            finally
            {
                ArrayPool.Return(boxArray);
                ArrayPool.Return(adjustedBoxArray);
            }
        });

        return yoloBoundingBoxes.ToList();
    }

    private int[] Xyxy2Xywh(IReadOnlyList<int> inputs, int imageHeight, int imageWidth)
    {
        return
        [
            inputs[0],
            inputs[1],
            Math.Min(inputs[2], imageWidth) - inputs[0],
            Math.Min(inputs[3], imageHeight) - inputs[1]
        ];
    }
}
