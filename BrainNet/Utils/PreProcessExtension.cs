using Microsoft.ML.OnnxRuntime.Tensors;

namespace BrainNet.Utils;

public static class PreProcessExtension
{
    public static (DenseTensor<float>, float[], float[]) LetterBox(this DenseTensor<float> image, bool auto, bool scaleFill, bool scaleUp, int stride, int[] shapes)
    {
        var oriShape = image.Dimensions[1..]; // HW
        int[] newShape = [3, shapes[0], shapes[1]];

        DenseTensor<float> feed = new DenseTensor<float>(dimensions: newShape);
        feed.Fill(114);

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
                feed[0, x + top, y + left] = image[0, x, y];
                feed[1, x + top, y + left] = image[1, x, y];
                feed[2, x + top, y + left] = image[2, x, y];
            }
        });

        return (feed, ratio, [dh, dw]);
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