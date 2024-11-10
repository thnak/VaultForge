using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BrainNet.Service.ObjectDetection;

public static class ImageExtension
{
    public static void PreprocessImage(this Image<Rgb24> image, DenseTensor<float> target)
    {
        var width = image.Width;
        var height = image.Height;

        var strideBatchR = target.Strides[0];
        var strideBatchG = target.Strides[0] + target.Strides[1] * 1;
        var strideBatchB = target.Strides[0] + target.Strides[1] * 2;

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
        tensorSpan[tensorIndex] = pixel.R;
    }

    private static void WritePixel(Span<Float16> tensorSpan, int tensorIndex, Rgb24 pixel)
    {
        tensorSpan[tensorIndex] = (Float16)pixel.R;
    }


    public static void Image2DenseTensor(this Image<Rgb24> image, DenseTensor<float> target)
    {
        var width = image.Width;
        var height = image.Height;
        Parallel.For(0, height, i =>
        {
            var rowSpan = image.DangerousGetPixelRowMemory(i).Span;

            for (int j = 0; j < width; j++)
            {
                target.WritePixelRgba(rowSpan, i, j);
            }
        });
    }

    private static void WritePixelRgba(this DenseTensor<float> tensorSpan, Span<Rgb24> pixel, int tensorIndexX, int tensorIndexY)
    {
        tensorSpan[0, tensorIndexX, tensorIndexY] = pixel[tensorIndexY].R;
        tensorSpan[1, tensorIndexX, tensorIndexY] = pixel[tensorIndexY].G;
        tensorSpan[2, tensorIndexX, tensorIndexY] = pixel[tensorIndexY].B;
    }

    public static void ProcessToTensor(this Image<Rgb24> image, Size modelSize, bool originalAspectRatio, DenseTensor<float> target, int batch)
    {
        var options = new ResizeOptions()
        {
            Size = modelSize,
            Mode = originalAspectRatio ? ResizeMode.Max : ResizeMode.Stretch,
        };

        var xPadding = (modelSize.Width - image.Width) / 2;
        var yPadding = (modelSize.Height - image.Height) / 2;

        var width = image.Width;
        var height = image.Height;

        // Pre-calculate strides for performance
        var strideBatchR = target.Strides[0] * batch + target.Strides[1] * 0;
        var strideBatchG = target.Strides[0] * batch + target.Strides[1] * 1;
        var strideBatchB = target.Strides[0] * batch + target.Strides[1] * 2;
        var strideY = target.Strides[2];
        var strideX = target.Strides[3];

        // Get a span of the whole tensor for fast access
        var tensorSpan = target.Buffer;

        // Try get continuous memory block of the entire image data
        if (image.DangerousTryGetSinglePixelMemory(out var memory))
        {
            Parallel.For(0, width * height, index =>
            {
                int x = index % width;
                int y = index / width;
                int tensorIndex = strideBatchR + strideY * (y + yPadding) + strideX * (x + xPadding);

                var pixel = memory.Span[index];
                WritePixel(tensorSpan.Span, tensorIndex, pixel, strideBatchR, strideBatchG, strideBatchB);
            });
        }
        else
        {
            Parallel.For(0, height, y =>
            {
                var rowSpan = image.DangerousGetPixelRowMemory(y).Span;
                int tensorYIndex = strideBatchR + strideY * (y + yPadding);

                for (int x = 0; x < width; x++)
                {
                    int tensorIndex = tensorYIndex + strideX * (x + xPadding);
                    var pixel = rowSpan[x];
                    WritePixel(tensorSpan.Span, tensorIndex, pixel, strideBatchR, strideBatchG, strideBatchB);
                }
            });
        }
    }

    private static void WritePixel(Span<float> tensorSpan, int tensorIndex, Rgb24 pixel, int strideBatchR, int strideBatchG, int strideBatchB)
    {
        tensorSpan[tensorIndex] = pixel.R / 255f;
        tensorSpan[tensorIndex + strideBatchG - strideBatchR] = pixel.G / 255f;
        tensorSpan[tensorIndex + strideBatchB - strideBatchR] = pixel.B / 255f;
    }
}