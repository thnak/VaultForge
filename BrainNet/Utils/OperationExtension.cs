using Microsoft.ML.OnnxRuntime.Tensors;

namespace BrainNet.Utils;

public static class OperationExtension
{
    public static DenseTensor<float> Div(this DenseTensor<float> tensor1, float maxPixelValue)
    {
        Parallel.For(0, tensor1.Dimensions[1], x =>
        {
            for (int y = 0; y < tensor1.Dimensions[2]; y++)
            {
                tensor1[0, x, y] /= maxPixelValue;
                tensor1[1, x, y] /= maxPixelValue;
                tensor1[2, x, y] /= maxPixelValue;
            }
        });
        return tensor1;
    }

    public static DenseTensor<float> Mul(this DenseTensor<float> tensor1, DenseTensor<float> tensor2)
    {
        DenseTensor<float> tensor3 = new DenseTensor<float>(tensor1.Dimensions);

        Parallel.For(0, tensor1.Dimensions[0], i =>
        {
            for (int j = 0; j < tensor1.Dimensions[1]; j++)
            {
                for (int k = 0; k < tensor1.Dimensions[1]; k++)
                {
                    tensor3[i, j] += tensor1[i, k] * tensor2[k, j];
                }
            }
        });

        return tensor3;
    }

    public static DenseTensor<float> ExpandDim(this DenseTensor<float> tensor)
    {
        int height = tensor.Dimensions[1];
        int width = tensor.Dimensions[2];
        int[] shape = new[] { 1, 3, height, width };

        DenseTensor<float> denseTensor = new DenseTensor<float>(dimensions: shape);
        Parallel.For(0, height, i =>
        {
            Parallel.For(0, width, x =>
            {
                denseTensor[0, 0, i, x] = tensor[0, i, x];
                denseTensor[0, 1, i, x] = tensor[1, i, x];
                denseTensor[0, 2, i, x] = tensor[2, i, x];
            });
        });

        return denseTensor;
    }

    public static void CopyMemoryToByteArray(Memory<float> memory, float[] byteArray)
    {
        // Create a byte array large enough to hold the float values
        
        // Get a span of the memory to access its values
        Span<float> span = memory.Span;

        // Convert each float to bytes and copy them into the byte array
        for (int i = 0; i < span.Length; i++)
        {
            // Use BitConverter to convert each float to a byte array
            byte[] floatBytes = BitConverter.GetBytes(span[i]);
            
            // Copy the bytes into the byteArray
            Array.Copy(floatBytes, 0, byteArray, i * sizeof(float), sizeof(float));
        }
    }
    
    /// <summary>
    /// concatenate 3D array into 4D array
    /// </summary>
    /// <param name="tensors"></param>
    /// <returns></returns>
    public static DenseTensor<float> Concat(this List<DenseTensor<float>> tensors)
    {
        int tensorsCount = tensors.Count;

        int[] fimFirst = tensors.First().Dimensions.ToArray();
        int rank = tensors.First().Rank;
        int[] dim = rank == 3 ? new[] { tensorsCount, fimFirst[0], fimFirst[1], fimFirst[2] } : new[] { tensorsCount, fimFirst[1], fimFirst[2], fimFirst[3] };

        DenseTensor<float> value = new DenseTensor<float>(dim);

        if (rank == 3)
        {
            Parallel.For(0, fimFirst[1], x =>
            {
                for (int i = 0; i < fimFirst[2]; i++)
                {
                    for (int y = 0; y < tensorsCount; y++)
                    {
                        value[y, 0, x, i] = tensors[y][0, x, i];
                        value[y, 1, x, i] = tensors[y][1, x, i];
                        value[y, 2, x, i] = tensors[y][2, x, i];
                    }
                }
            });
        }
        else
        {
            Parallel.For(0, fimFirst[2], x =>
            {
                for (int i = 0; i < fimFirst[3]; i++)
                {
                    for (int y = 0; y < tensorsCount; y++)
                    {
                        value[y, 0, x, i] = tensors[y][0, 0, x, i];
                        value[y, 1, x, i] = tensors[y][0, 1, x, i];
                        value[y, 2, x, i] = tensors[y][0, 2, x, i];
                    }
                }
            });
        }


        return value;
    }

    // public static DenseTensor<Float16> Cast2Float16(this DenseTensor<float> tensor)
    // {
    //     var array = tensor.ToArray().Select(x => (Float16)x).ToArray();
    //     DenseTensor<float> tensor2 = new DenseTensor<float>(tensor.Dimensions);
    //     for (int i = 0; i < UPPER; i++)
    //     {
    //         
    //     }
    // }
}