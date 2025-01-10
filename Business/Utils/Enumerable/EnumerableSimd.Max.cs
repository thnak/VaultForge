using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Business.Utils.Enumerable;

public static partial class EnumerableSimd
{
    /// <summary>
    /// Retrieves the maximum value of the list.
    /// </summary>
    /// <returns>The max as type T.</returns>
    public static T Max<T>(this List<T> list) where T : unmanaged, IMinMaxValue<T>, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(list);
        var span = CollectionsMarshal.AsSpan(list);

        return Max(span);
    }

    /// <summary>
    /// Retrieves the maximum value of the array.
    /// </summary>
    /// <returns>The max as type T.</returns>
    public static T Max<T>(this T[] array) where T : unmanaged, IMinMaxValue<T>, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(array);
        return Max(array.AsSpan());
    }

    /// <summary>
    /// Retrieves the maximum value from a lazy source using chunk processing and SIMD.
    /// </summary>
    /// <typeparam name="T">The numeric type (e.g., int, float, double).</typeparam>
    /// <param name="source">The lazy enumerable source.</param>
    /// <param name="bufferSize">The size of the buffer for chunk processing.</param>
    /// <returns>The maximum value.</returns>
    public static T Max<T>(IEnumerable<T> source, int bufferSize = 1024) where T : unmanaged, IMinMaxValue<T>, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(source);

        // Rent a buffer using ArrayPool for efficient memory use
        T[] buffer = ArrayPool<T>.Shared.Rent(bufferSize);
        int vectorSize = Vector<T>.Count;

        T overallMax = T.MinValue;

        using (var enumerator = source.GetEnumerator())
        {
            int count;
            while ((count = FillBuffer(enumerator, buffer)) > 0)
            {
                int i = 0;
                var vectorMax = new Vector<T>(buffer[0]); // Initialize with the first element in the buffer

                // Process full vector-sized chunks
                for (; i <= count - vectorSize; i += vectorSize)
                {
                    var currentVector = new Vector<T>(buffer, i);
                    vectorMax = Vector.Max(vectorMax, currentVector);
                }

                // Find the maximum in the vector
                for (int j = 0; j < vectorSize; j++)
                {
                    overallMax = T.Max(overallMax, vectorMax[j]);
                }

                // Process remaining elements in the buffer
                for (; i < count; i++)
                {
                    overallMax = T.Max(overallMax, buffer[i]);
                }
            }
        }

        // Return the buffer to the pool
        ArrayPool<T>.Shared.Return(buffer, true);

        return overallMax;
    }


    /// <summary>
    /// Retrieves the maximum value of the memory.
    /// </summary>
    /// <returns>The max as type T.</returns>
    public static T Max<T>(this Memory<T> memory) where T : unmanaged, IMinMaxValue<T>, INumber<T> => Max(memory.Span);

    /// <summary>
    /// Retrieves the maximum value of the span.
    /// </summary>
    /// <returns>The max as type T.</returns>
    public static T Max<T>(this Span<T> span) where T : unmanaged, IMinMaxValue<T>, INumber<T>
    {
        if (span.IsEmpty)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }

        var spanAsVectors = MemoryMarshal.Cast<T, Vector<T>>(span);
        var maxVector = VectorHelper.CreateWithValue(T.MinValue);

        for (var i = 0; i < spanAsVectors.Length - 1; i += 2)
        {
            var iterationMax = Vector.Max(spanAsVectors[i], spanAsVectors[i + 1]);
            maxVector = Vector.Max(maxVector, iterationMax);
        }

        if (spanAsVectors.Length % 2 == 1)
        {
            maxVector = Vector.Max(maxVector, spanAsVectors[^1]);
        }

        var remainingElements = span.Length % Vector<T>.Count;
        if (remainingElements > 0)
        {
            Span<T> lastVectorElements = stackalloc T[Vector<T>.Count];
            lastVectorElements.Fill(T.MinValue);
            span[^remainingElements..].CopyTo(lastVectorElements);
            maxVector = Vector.Max(maxVector, new Vector<T>(lastVectorElements));
        }

        var minValue = T.MinValue;
        for (var i = 0; i < Vector<T>.Count; i++)
        {
            minValue = T.Max(minValue, maxVector[i]);
        }

        return minValue;
    }
}