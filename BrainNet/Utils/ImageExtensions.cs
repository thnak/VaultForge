using System.Diagnostics.CodeAnalysis;
using System.Numerics.Tensors;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tensor = System.Numerics.Tensors.Tensor;

namespace BrainNet.Utils;

public static class ImageExtensions
{
    [Experimental("SYSLIB5001")]
    public static void SampleCompute()
    {
        // Create a tensor (1 x 3).
        System.Numerics.Tensors.Tensor<int> t0 = Tensor.Create([1, 2, 3], [1, 3]); // [[1, 2, 3]]

// Reshape tensor (3 x 1).
        System.Numerics.Tensors.Tensor<int> t1 = t0.Reshape(3, 1); // [[1], [2], [3]]

// Slice tensor (2 x 1).
        System.Numerics.Tensors.Tensor<int> t2 = t1.Slice(1.., ..); // [[2], [3]]

// Broadcast tensor (3 x 1) -> (3 x 3).
// [
//  [ 1, 1, 1],
//  [ 2, 2, 2],
//  [ 3, 3, 3]
// ]
        var t3 = Tensor.Broadcast<int>(t1, [3, 3]);

// Math operations.
        var t4 = Tensor.Add(t0, 1); // [[2, 3, 4]]
        var t5 = Tensor.Add(t0.AsReadOnlyTensorSpan(), t0); // [[2, 4, 6]]
        var t6 = Tensor.Subtract(t0, 1); // [[0, 1, 2]]
        var t7 = Tensor.Subtract(t0.AsReadOnlyTensorSpan(), t0); // [[0, 0, 0]]
        var t8 = Tensor.Multiply(t0, 2); // [[2, 4, 6]]
        var t9 = Tensor.Multiply(t0.AsReadOnlyTensorSpan(), t0); // [[1, 4, 9]]
        var t10 = Tensor.Divide(t0, 2); // [[0.5, 1, 1.5]]
        var t11 = Tensor.Divide(t0.AsReadOnlyTensorSpan(), t0); // [[1, 1, 1]]
    }

    [Experimental("SYSLIB5001")]
    public static ReadOnlyTensorSpan<float> Device(this float[] array)
    {
        System.Numerics.Tensors.Tensor<float> tensor = Tensor.Create(array, []);
        var result = Tensor.Divide(tensor, 2f);
        return result.AsReadOnlyTensorSpan();
    }

    public static DenseTensor<float> Image2DenseTensor(Image<Rgb24> image)
    {
        int[] shape = new[] { 3, image.Height, image.Width };

        DenseTensor<float> feed = new DenseTensor<float>(shape);

        Parallel.For(0, shape[1], y =>
        {
            for (var x = 0; x < shape[2]; x++)
            {
                feed[0, y, x] = image[x, y].R;
                feed[1, y, x] = image[x, y].G;
                feed[2, y, x] = image[x, y].B;
            }
        });

        return feed;
    }
}