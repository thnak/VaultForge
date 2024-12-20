using System.Collections.Concurrent;
using BrainNet.Utils;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.ObjectDetection.Model.Feeder;

public class YoloFeeder
{
    private ConcurrentBag<DenseTensor<float>> FeedQueue { get; set; } = new();
    private ConcurrentBag<int[]> ImageShape { get; set; } = [];
    private ConcurrentBag<float[]> Ratios { get; set; } = [];
    private ConcurrentBag<float[]> Dwdhs { get; set; } = [];
    private int[] OutPutShape { get; set; } = [];
    private int Stride { get; set; }

    public YoloFeeder(int[] outPutShape, int stride)
    {
        OutPutShape = outPutShape;
        Stride = stride;
    }

    public void Clear()
    {
        FeedQueue.Clear();
        ImageShape.Clear();
        Ratios.Clear();
        Dwdhs.Clear(); 
    }
    
    public void SetTensor(DenseTensor<float> tensor)
    {
        var imageShape = tensor.Dimensions[1..].ToArray();
        var lettered = tensor.LetterBox(false, false, true, Stride, OutPutShape);
        var feedTensor = lettered.Item1.Div(255f).ExpandDim();
        FeedQueue.Add(feedTensor);
        Dwdhs.Add(lettered.Item3);
        Ratios.Add(lettered.Item2);
        ImageShape.Add(imageShape);
    }

    public void SetTensor(Image<Rgb24> image)
    {
        DenseTensor<float> tensor = new DenseTensor<float>([3, image.Height, image.Width]);
        image.Image2DenseTensor(tensor);
        int[] imageShape = [image.Height, image.Width];
        var lettered = tensor.LetterBox(false, false, true, Stride, OutPutShape);
        var feedTensor = lettered.Item1.Div(255f).ExpandDim();
        FeedQueue.Add(feedTensor);
        Dwdhs.Add(lettered.Item3);
        Ratios.Add(lettered.Item2);
        ImageShape.Add(imageShape);
    }
    
    public void SetTensor(string path)
    {
        Image<Rgb24> image = Image.Load<Rgb24>(path);
        SetTensor(image);
    }

    public (DenseTensor<float> tensor, List<float[]> dwdhs, List<float[]> ratios, List<int[]> imageShape) GetBatchTensor()
    {
        DenseTensor<float> tensor = FeedQueue.ToList().Concat();
        return (tensor, Dwdhs.ToList(), Ratios.ToList(), ImageShape.ToList());
    }

    public IEnumerable<(DenseTensor<float> tensor, float[] dwdhs, float[] ratios, int[] imageShape)> GetTensor()
    {
        var feeds = FeedQueue.ToList();
        var dwdhs = Dwdhs.ToList();
        var ratios = Ratios.ToList();
        var imageShape = ImageShape.ToList();
        for (int i = 0; i < Dwdhs.Count; i++)
        {
            yield return (feeds[i], dwdhs[i], ratios[i], imageShape[i]);
        }
    }
}