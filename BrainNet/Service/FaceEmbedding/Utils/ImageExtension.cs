using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace BrainNet.Service.FaceEmbedding.Utils;

public static class ImageExtension
{
    public static DenseTensor<float> Image2DenseTensor(Image<Rgb24> image)
    {
        int[] shape = { 3, image.Height, image.Width };

        DenseTensor<float> feed = new DenseTensor<float>(shape);

        Parallel.For(0, shape[1], y =>
        {
            for (var x = 0; x < shape[2]; x++)
            {
                feed[0, y, x] = image[x, y].R / 256f;
                feed[1, y, x] = image[x, y].G / 256f;
                feed[2, y, x] = image[x, y].B / 256f;
            }
        });

        return feed;
    }

    public static void PreprocessImage(this Image<Rgb24> image, DenseTensor<float> target)
    {
        var width = image.Width;
        var height = image.Height;

        var tensorSpan = target.Buffer;

        if (image.DangerousTryGetSinglePixelMemory(out var memory))
        {
            Parallel.For(0, width * height, index =>
            {
                var pixel = memory.Span[index];
                WritePixel(tensorSpan.Span, index, pixel);
            });
        }
        else
        {
            Parallel.For(0, height, y =>
            {
                var rowSpan = image.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < width; x++)
                {
                    var pixel = rowSpan[x];
                    WritePixel(tensorSpan.Span, x, pixel);
                }
            });
        }
    }

    public static void PreprocessImage(this Image<Rgb24> image, DenseTensor<Float16> target)
    {
        var width = image.Width;
        var height = image.Height;

        var tensorSpan = target.Buffer;

        if (image.DangerousTryGetSinglePixelMemory(out var memory))
        {
            Parallel.For(0, width * height, index =>
            {
                var pixel = memory.Span[index];
                WritePixel(tensorSpan.Span, index, pixel);
            });
        }
        else
        {
            Parallel.For(0, height, y =>
            {
                var rowSpan = image.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < width; x++)
                {
                    var pixel = rowSpan[x];
                    WritePixel(tensorSpan.Span, x, pixel);
                }
            });
        }
    }

    private static void WritePixel(Span<float> tensorSpan, int tensorIndex, Rgb24 pixel)
    {
        tensorSpan[tensorIndex] = pixel.R / 255f;
    }

    private static void WritePixel(Span<Float16> tensorSpan, int tensorIndex, Rgb24 pixel)
    {
        tensorSpan[tensorIndex] = (Float16)(pixel.R / 255f);
    }
}