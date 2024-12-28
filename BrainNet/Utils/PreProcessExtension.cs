using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace BrainNet.Utils;

public static class PreProcessExtension
{
    public static (DenseTensor<float>, float[], float[]) LetterBox(this DenseTensor<float> image, bool auto, bool scaleFill, bool scaleUp, int stride, int[] shapes)
    {
        var oriShape = image.Dimensions[1..]; // HW
        int[] newShape = [3, shapes[0], shapes[1]];

        DenseTensor<float> feed = new DenseTensor<float>(dimensions: newShape);
        feed.FillTensor(114);

        float r = Math.Min((float)shapes[0] / oriShape[0], (float)shapes[1] / oriShape[1]);
        if (!scaleUp)
        {
            r = Math.Min(r, 1.0f);
        }

        float[] ratio = [r, r];
        int newHeight = (int)Math.Round(oriShape[0] * r);
        int newWidth = (int)Math.Round(oriShape[1] * r);
        float dw = (shapes[1] - newWidth) / 2f;
        float dh = (shapes[0] - newHeight) / 2f;

        if (auto)
        {
            dw %= stride;
            dh %= stride;
        }
        else if (scaleFill)
        {
            dw = 0f;
            dh = 0f;
            newHeight = shapes[0];
            newWidth = shapes[1];
            ratio = [(float)shapes[0] / oriShape[0], (float)shapes[1] / oriShape[1]];
        }

        if (newHeight != oriShape[0] || newWidth != oriShape[1])
        {
            image = ResizeLinear(image, [newHeight, newWidth]);
        }

        int left = (int)Math.Round(dw - 0.1);
        int top = (int)Math.Round(dh - 0.1);

        // Parallelize the copying process
        Parallel.For(0, image.Dimensions[1], x =>
        {
            for (int y = 0; y < image.Dimensions[2]; y++)
            {
                var xAxis = x + top;
                var yAxis = y + left;
                feed[0, xAxis, yAxis] = image[0, x, y];
                feed[1, xAxis, yAxis] = image[1, x, y];
                feed[2, xAxis, yAxis] = image[2, x, y];
            }
        });

        return (feed, ratio, [dh, dw]);
    }


    public static Mat Letterbox(
        this Mat image,
        Size newShape = default,
        Scalar color = default,
        bool auto = true,
        bool scaleFill = false,
        bool scaleUp = true,
        int stride = 32)
    {
        if (newShape == default)
            newShape = new Size(640, 640);

        if (color == default)
            color = new Scalar(114, 114, 114);

        // Current shape
        int originalHeight = image.Rows;
        int originalWidth = image.Cols;

        // Scale ratio (new / old)
        double r = Math.Min(newShape.Height / (double)originalHeight, newShape.Width / (double)originalWidth);
        if (!scaleUp)
            r = Math.Min(r, 1.0);

        // Compute padding
        // double ratioWidth = r, ratioHeight = r;
        int newUnpadWidth = (int)Math.Round(originalWidth * r);
        int newUnpadHeight = (int)Math.Round(originalHeight * r);
        double dw = newShape.Width - newUnpadWidth;
        double dh = newShape.Height - newUnpadHeight;

        if (auto)
        {
            dw %= stride;
            dh %= stride;
        }
        else if (scaleFill)
        {
            dw = 0.0;
            dh = 0.0;
            newUnpadWidth = newShape.Width;
            newUnpadHeight = newShape.Height;
            // ratioWidth = newShape.Width / (double)originalWidth;
            // ratioHeight = newShape.Height / (double)originalHeight;
        }

        dw /= 2;
        dh /= 2;

        // Resize
        Mat resizedImage = new Mat();
        Cv2.Resize(image, resizedImage, new Size(newUnpadWidth, newUnpadHeight), interpolation: InterpolationFlags.Linear);

        // Add border
        int top = (int)Math.Round(dh - 0.1);
        int bottom = (int)Math.Round(dh + 0.1);
        int left = (int)Math.Round(dw - 0.1);
        int right = (int)Math.Round(dw + 0.1);

        Mat borderedImage = new Mat();
        Cv2.CopyMakeBorder(resizedImage, borderedImage, top, bottom, left, right, BorderTypes.Constant, color);

        return borderedImage;
    }


    public static void FillTensorA2B(this DenseTensor<float> feed, DenseTensor<float> image, int offsetLeft, int offsetTop)
    {
        Parallel.For(0, image.Dimensions[1], x =>
        {
            for (int y = 0; y < image.Dimensions[2]; y++)
            {
                var xAxis = x + offsetTop;
                var yAxis = y + offsetLeft;
                feed[0, xAxis, yAxis] = image[0, x, y];
                feed[1, xAxis, yAxis] = image[1, x, y];
                feed[2, xAxis, yAxis] = image[2, x, y];
            }
        });
    }

    public static void FillTensor(this DenseTensor<float> feed, float value)
    {
        Parallel.For(0, feed.Dimensions[1], x =>
        {
            for (int y = 0; y < feed.Dimensions[2]; y++)
            {
                feed[0, x, y] = value;
                feed[1, x, y] = value;
                feed[2, x, y] = value;
            }
        });
    }

    public static DenseTensor<float> ResizeLinear(this DenseTensor<float> imageMatrix, int[] shape)
    {
        int[] newShape = [3, shape[0], shape[1]];
        DenseTensor<float> outputImage = new DenseTensor<float>(newShape);

        int originalHeight = imageMatrix.Dimensions[1];
        int originalWidth = imageMatrix.Dimensions[2];

        float invScaleFactorY = (float)originalHeight / shape[0];
        float invScaleFactorX = (float)originalWidth / shape[1];

        Parallel.For(0, shape[0], y =>
        {
            float oldY = y * invScaleFactorY;
            int yFloor = (int)Math.Floor(oldY);
            int yCeil = Math.Min(originalHeight - 1, (int)Math.Ceiling(oldY));
            float yFraction = oldY - yFloor;

            for (int x = 0; x < shape[1]; x++)
            {
                float oldX = x * invScaleFactorX;
                int xFloor = (int)Math.Floor(oldX);
                int xCeil = Math.Min(originalWidth - 1, (int)Math.Ceiling(oldX));
                float xFraction = oldX - xFloor;

                for (int c = 0; c < 3; c++)
                {
                    // Sample four neighboring pixels
                    float topLeft = imageMatrix[c, yFloor, xFloor];
                    float topRight = imageMatrix[c, yFloor, xCeil];
                    float bottomLeft = imageMatrix[c, yCeil, xFloor];
                    float bottomRight = imageMatrix[c, yCeil, xCeil];

                    // Interpolate between the four neighboring pixels
                    float topBlend = topLeft + xFraction * (topRight - topLeft);
                    float bottomBlend = bottomLeft + xFraction * (bottomRight - bottomLeft);
                    float finalBlend = topBlend + yFraction * (bottomBlend - topBlend);

                    // Assign the interpolated value to the output image
                    outputImage[c, y, x] = finalBlend;
                }
            }
        });

        return outputImage;
    }

    public static List<string> ChunkTextByDelimiter(string text, char delimiter)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty.", nameof(text));

        return text.Split(delimiter).Select(chunk => chunk.Trim()).ToList();
    }
}