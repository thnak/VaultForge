using System.Numerics;

namespace Business.Utils;

public static class ByteExtensions
{
    public static byte[] XorParity(this byte[] data0, byte[] data1)
    {
        int vectorSize = Vector<byte>.Count;
        int i = 0;

        byte[] parity = new byte[data0.Length];

        // Process in chunks of Vector<byte>.Count (size of SIMD vector)
        if (Vector.IsHardwareAccelerated)
        {
            for (; i <= data1.Length - vectorSize; i += vectorSize)
            {
                // Load the current portion of the parity and data as vectors
                var data0Vector = new Vector<byte>(data0, i);
                var data1Vector = new Vector<byte>(data1, i);

                // XOR the vectors
                var resultVector = data0Vector ^ data1Vector;

                // Store the result back into the parity array
                resultVector.CopyTo(parity, i);
            }

            return parity;
        }

        // Fallback to scalar XOR for the remaining bytes (if any)
        for (; i < data1.Length; i++)
        {
            parity[i] = (byte)(data0[i] ^ data1[i]);
        }

        return parity;
    }

    public static byte[] XorParity(this byte[][] data)
    {
        int length = data.First().Length;

        // Initialize the result array for storing the XOR parity
        byte[] result = new byte[length];
        data.First().CopyTo(result, 0);

        for (int i = 1; i < data.Length; i++)
        {
            result.XorParity(data[i]).CopyTo(result, 0);
        }

        return result;
    }
}