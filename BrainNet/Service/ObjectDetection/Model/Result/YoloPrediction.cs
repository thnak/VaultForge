using System.Collections.Concurrent;

namespace BrainNet.Service.ObjectDetection.Model.Result;

using System;
using System.Collections.Generic;
using System.Linq;

public readonly ref struct YoloPrediction(Span<float> predictionArrayResults, int bufferLength, string[] categories, List<float[]> padHeightAndWidths, List<float[]> ratios, List<int[]> imageShapes)
{
    private Span<float> PredictionArrays { get; } = predictionArrayResults;
    private string[] Categories { get; } = categories;
    private List<float[]> PadHeightAndWidths { get; } = padHeightAndWidths;
    private List<int[]> ImageShapes { get; } = imageShapes;
    private List<float[]> Ratios { get; } = ratios;
    private int BufferLength { get; } = bufferLength;

    public YoloPrediction(ReadOnlySpan<float> predictionArrayResults, int bufferLength, string[] categories, List<float[]> padHeightAndWidths, List<float[]> ratios, List<int[]> imageShapes) : this(predictionArrayResults.ToArray(), bufferLength, categories, padHeightAndWidths, ratios, imageShapes)
    {
    }

    public List<YoloBoundingBox> GetDetect()
    {
        int length = BufferLength / 7;
        if (length == 0) return new List<YoloBoundingBox>();

        var yoloBoundingBoxes = new ConcurrentBag<YoloBoundingBox>();

        Span<float> boxArrayAlloc = stackalloc float[4 * length];
        Span<float> adjustedBoxArrayAlloc = stackalloc float[boxArrayAlloc.Length];
        for (int i = 0; i < length; i++)
        {
            var slice = PredictionArrays.Slice(i * 7, 7);

            Span<float> boxArray = boxArrayAlloc.Slice(i * 4, 4);
            Span<float> adjustedBoxArray = adjustedBoxArrayAlloc.Slice(i * 4, 4);

            var batchId = (int)slice[0];
            // Extract box coordinates
            slice.Slice(1, 4).CopyTo(boxArray);
            var clsIdx = (int)slice[5];

            // Adjust box using padding
            if (batchId >= PadHeightAndWidths.Count)
                continue;

            var pad = PadHeightAndWidths[batchId];
            var ratios = Ratios[batchId];
            var oriShapes = ImageShapes[batchId];

            for (int j = 0; j < 4; j++)
            {
                boxArray[j] -= j % 2 == 0 ? pad[1] : pad[0];
                adjustedBoxArray[j] = Math.Max(boxArray[j] / ratios[0], 0);
            }

            var box = new[]
            {
                (int)adjustedBoxArray[0],
                (int)adjustedBoxArray[1],
                (int)adjustedBoxArray[2],
                (int)adjustedBoxArray[3]
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