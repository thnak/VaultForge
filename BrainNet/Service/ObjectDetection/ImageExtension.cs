using System.Runtime.CompilerServices;
using BrainNet.Models.Vector;
using BrainNet.Service.ObjectDetection.Model.Result;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using PointF = SixLabors.ImageSharp.PointF;
using RectangleF = SixLabors.ImageSharp.RectangleF;
using Size = SixLabors.ImageSharp.Size;

namespace BrainNet.Service.ObjectDetection;

public static class ImageExtension
{
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

    private static void ProcessToTensorCore(Image<Rgb24> image, MemoryTensor<float> tensor, VectorPosition<int> padding)
    {
        var width = image.Width;
        var height = image.Height;

        // Pre-calculate strides for performance
        var strideY = tensor.Strides[2];
        var strideX = tensor.Strides[3];
        var strideR = tensor.Strides[1] * 0;
        var strideG = tensor.Strides[1] * 1;
        var strideB = tensor.Strides[1] * 2;

        var padG = strideG - strideR;
        var padB = strideB - strideR;

        // Get a span of the whole tensor for fast access
        var tensorSpan = tensor.Span;

        // Try get continuous memory block of the entire image data
        if (image.DangerousTryGetSinglePixelMemory(out var memory))
        {
            var pixels = memory.Span;
            var length = height * width;

            for (var index = 0; index < length; index++)
            {
                var x = index % width;
                var y = index / width;

                var tensorIndex = strideR + strideY * (y + padding.Y) + strideX * (x + padding.X);

                var pixel = pixels[index];

                WriteSpanPixel(tensorSpan, tensorIndex, pixel, padG, padB);
            }
        }
        else
        {
            for (var y = 0; y < height; y++)
            {
                var rowSpan = image.DangerousGetPixelRowMemory(y).Span;
                var tensorYIndex = strideR + strideY * (y + padding.Y);

                for (var x = 0; x < width; x++)
                {
                    var tensorIndex = tensorYIndex + strideX * (x + padding.X);
                    var pixel = rowSpan[x];

                    WriteSpanPixel(tensorSpan, tensorIndex, pixel, padG, padB);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSpanPixel(Span<float> target, int index, Rgb24 pixel, int strideBatchR, int strideBatchG, int strideBatchB)
    {
        target[index] = pixel.R / 255f;
        target[index + strideBatchG - strideBatchR] = pixel.G / 255f;
        target[index + strideBatchB - strideBatchR] = pixel.B / 255f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteSpanPixel(Span<float> target, int index, Rgb24 pixel, int padG, int padB)
    {
        target[index] = pixel.R / 255f;
        target[index + padG] = pixel.G / 255f;
        target[index + padB] = pixel.B / 255f;
    }

    public static void NormalizeInput(this Image<Rgb24> image, MemoryTensor<float> target, Size imageSize, float[] ratios, float[] pads, bool keepAspectRatio = false)
    {
        // Resize the input image
        using var resized = ResizeImage(image, out var padding, imageSize, keepAspectRatio);
        ratios[0] = (float)resized.Height / image.Height;
        ratios[1] = (float)resized.Width / image.Width;
        pads[0] = padding.Y;
        pads[1] = padding.X;
        // Process the image to tensor
        ProcessToTensorCore(resized, target, padding);
    }

    private static Image<Rgb24> ResizeImage(Image<Rgb24> image, out VectorPosition<int> padding, Size imageSize, bool keepAspectRatio = false)
    {
        // Get the model image input size
        var inputSize = imageSize;

        // Create resize options
        var options = new ResizeOptions()
        {
            Size = inputSize,

            // Select resize mode according to 'KeepAspectRatio'
            Mode = keepAspectRatio ? ResizeMode.Max : ResizeMode.Stretch,

            // Select faster resampling algorithm
            Sampler = KnownResamplers.NearestNeighbor
        };

        // Create resized image
        var resized = image.Clone(x => x.Resize(options));

        // Calculate padding
        padding =
        (
            (inputSize.Width - resized.Size.Width) / 2,
            (inputSize.Height - resized.Size.Height) / 2
        );

        // Return the resized image
        return resized;
    }

    private static void WritePixelRgba(this DenseTensor<float> tensorSpan, Span<Rgb24> pixel, int tensorIndexX, int tensorIndexY)
    {
        tensorSpan[0, tensorIndexX, tensorIndexY] = pixel[tensorIndexY].R;
        tensorSpan[1, tensorIndexX, tensorIndexY] = pixel[tensorIndexY].G;
        tensorSpan[2, tensorIndexX, tensorIndexY] = pixel[tensorIndexY].B;
    }

    public static void ProcessToTensor(this Image<Rgb24> image, Size modelSize, bool originalAspectRatio, DenseTensor<float> target, int batch)
    {
        var xPadding = (modelSize.Width - image.Width) / 2;
        var yPadding = (modelSize.Height - image.Height) / 2;

        var width = image.Width;
        var height = image.Height;

        // Pre-calculate strides for performance
        var strideBatchR = target.Strides[0] * batch;
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

    public static Image<Rgb24> PlotImage(this Image<Rgb24> src, SixLabors.Fonts.Font font, List<YoloBoundingBox> boundingBoxes)
    {
        // Define a font for text annotations.
        var textOption = new TextOptions(font);

        var newImage = src.Clone();

        foreach (var box in boundingBoxes)
        {
            // Extract bounding box coordinates and dimensions.
            var x = box.X;
            var y = box.Y;
            var width = box.Width;
            var height = box.Height;

            // Define the color for the rectangle and text.
            var boxColor = Color.Red;
            var textColor = Color.White;

            // Draw the bounding box.
            newImage.Mutate(ctx =>
            {
                // Draw the rectangle for the bounding box.
                ctx.Draw(boxColor, 2.0f, new RectangleF(x, y, width, height));

                // Prepare the text label with category name and score.
                var label = $"{box.ClassName} ({box.Score:P2})";

                // Measure the text size to create a background rectangle for better visibility.
                var textSize = TextMeasurer.MeasureBounds(label, textOption);
                var backgroundRectangle = new RectangleF(x, y - textSize.Height - 4, textSize.Width + 4, textSize.Height + 4);

                // Draw the background rectangle for the text.
                ctx.Fill(Color.Black, backgroundRectangle);

                // Draw the label text.
                ctx.DrawText(label, font, textColor, new PointF(x + 2, y - textSize.Height - 2));
            });
        }

        return newImage;
    }
}