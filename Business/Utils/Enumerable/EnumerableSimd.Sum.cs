using System.Buffers;
using System.Numerics;

namespace Business.Utils.Enumerable;

public static partial class EnumerableSimd
{
    public static T Sum<T>(T[] values) where T : struct, INumber<T>
    {
        int vectorSize = Vector<T>.Count;
        int i = 0;
        int length = values.Length;

        // Accumulate partial sums in a vector
        Vector<T> vectorSum = Vector<T>.Zero;

        // Process values in chunks of 'vectorSize'
        var limitStep1 = length - vectorSize;
        for (; i <= limitStep1; i += vectorSize)
        {
            var vector = new Vector<T>(values, i);
            vectorSum += vector;
        }

        // Sum the elements of the vectorSum
        T result = T.Zero;
        for (int j = 0; j < vectorSize; j++)
        {
            result += vectorSum[j];
        }

        // Sum any remaining elements
        for (; i < length; i++)
        {
            result += values[i];
        }

        return result;
    }


    public static T Sum<T>(IEnumerable<T> source, int bufferSize = 1024) where T : struct, INumber<T>
    {
        // Create a buffer using ArrayPool for efficiency
        T[] buffer = ArrayPool<T>.Shared.Rent(bufferSize);
        int vectorSize = Vector<T>.Count;

        Vector<T> vectorSum = Vector<T>.Zero;
        T result = T.Zero;

        int count;
        using (var enumerator = source.GetEnumerator())
        {
            while ((count = FillBuffer(enumerator, buffer)) > 0)
            {
                int i = 0;

                // Process full vector-sized chunks
                for (; i <= count - vectorSize; i += vectorSize)
                {
                    var vector = new Vector<T>(buffer, i);
                    vectorSum += vector;
                }

                // Sum remaining elements in the buffer
                for (; i < count; i++)
                {
                    result += buffer[i];
                }
            }
        }

        // Sum the elements of the final vectorSum
        for (int j = 0; j < vectorSize; j++)
        {
            result += vectorSum[j];
        }

        // Return the buffer to the pool
        ArrayPool<T>.Shared.Return(buffer, true);

        return result;
    }
}