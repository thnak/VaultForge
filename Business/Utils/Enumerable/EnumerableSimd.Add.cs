using System.Buffers;
using System.Numerics;

namespace Business.Utils.Enumerable;

public static partial class EnumerableSimd
{
    public static T[] Add<T>(this T[] left, T[] right) where T : struct, INumber<T>
    {
        if (left.Length != right.Length)
        {
            throw new ArgumentException($"{nameof(left)} and {nameof(right)} are not the same length");
        }

        int length = left.Length;
        T[] result = ArrayPool<T>.Shared.Rent(length);

        // Get the number of elements that can't be processed in the vector
        // NOTE: Vector<T>.Count is a JIT time constant and will get optimized accordingly
        int remaining = length % Vector<T>.Count;

        for (int i = 0; i < length - remaining; i += Vector<T>.Count)
        {
            var v1 = new Vector<T>(left, i);
            var v2 = new Vector<T>(right, i);
            (v1 + v2).CopyTo(result, i);
        }

        for (int i = length - remaining; i < length; i++)
        {
            result[i] = left[i] + right[i];
        }

        ArrayPool<T>.Shared.Return(result);

        return result;
    }

    public static double Mean<T>(this IEnumerable<T> source, int bufferSize = 1024) where T : struct, INumber<T>
    {
        // Rent a buffer from the ArrayPool
        T[] buffer = ArrayPool<T>.Shared.Rent(bufferSize);
        int vectorSize = Vector<T>.Count;

        Vector<T> vectorSum = Vector<T>.Zero;
        T scalarSum = T.Zero;
        long totalCount = 0;

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
                    scalarSum += buffer[i];
                }

                totalCount += count;
            }
        }

        // Sum the elements of the final vectorSum
        for (int j = 0; j < vectorSize; j++)
        {
            scalarSum += vectorSum[j];
        }

        // Return the buffer to the pool
        ArrayPool<T>.Shared.Return(buffer);

        // Calculate and return the mean
        if (totalCount == 0)
            return 0;

        return double.CreateChecked(scalarSum) / totalCount;
    }

    public static void Add<T>(T[] array, T number) where T : struct, INumber<T>
    {
        if (array.Length == 0)
            return;

        // Get a Span<T> over the array
        Span<T> span = array.AsSpan();
        int vectorSize = Vector<T>.Count;

        int i = 0;

        // Vectorized addition
        var numberVector = new Vector<T>(number);
        for (; i <= span.Length - vectorSize; i += vectorSize)
        {
            var currentVector = new Vector<T>(span.Slice(i, vectorSize));
            var resultVector = currentVector + numberVector;
            resultVector.CopyTo(span.Slice(i));
        }

        // Process any remaining elements
        for (; i < span.Length; i++)
        {
            span[i] += number;
        }
    }


    public static void Subtract<T>(T[] array, T number) where T : struct, INumber<T>
    {
        if (array.Length == 0)
            return;

        // Get a Span<T> over the array
        Span<T> span = array.AsSpan();
        int vectorSize = Vector<T>.Count;

        int i = 0;

        // Vectorized addition
        var numberVector = new Vector<T>(number);
        for (; i <= span.Length - vectorSize; i += vectorSize)
        {
            var currentVector = new Vector<T>(span.Slice(i, vectorSize));
            var resultVector = currentVector - numberVector;
            resultVector.CopyTo(span.Slice(i));
        }

        // Process any remaining elements
        for (; i < span.Length; i++)
        {
            span[i] -= number;
        }
    }

    /// <summary>
    /// Fills the buffer from the enumerator and returns the count of items filled.
    /// </summary>
    private static int FillBuffer<T>(IEnumerator<T> enumerator, T[] buffer)
    {
        int index = 0;
        while (index < buffer.Length && enumerator.MoveNext())
        {
            buffer[index++] = enumerator.Current;
        }

        return index;
    }
}