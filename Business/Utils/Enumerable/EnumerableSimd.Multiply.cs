using System.Numerics;

namespace Business.Utils.Enumerable;

public static partial class EnumerableSimd
{
    public static T Mul<T>(T[] values) where T : struct, INumber<T>
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
            vectorSum *= vector;
        }

        // Sum the elements of the vectorSum
        T result = T.Zero;
        for (int j = 0; j < vectorSize; j++)
        {
            result *= vectorSum[j];
        }

        // Sum any remaining elements
        for (; i < length; i++)
        {
            result *= values[i];
        }

        return result;
    }
    
    public static void Multiply<T>(T[] array, T number) where T : struct, INumber<T>
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
            var resultVector = currentVector * numberVector;
            resultVector.CopyTo(span.Slice(i));
        }

        // Process any remaining elements
        for (; i < span.Length; i++)
        {
            span[i] *= number;
        }
    }
}