using System.Linq.Expressions;
using Business.Utils.ExpressionExtensions;

namespace Business.Utils.StringExtensions;

public static class StringExtension
{
    public static string GetCacheKey<T>(this Expression<Func<T, bool>> expression) where T : class
    {
        var expressionStringBuilder = new ExpressionStringBuilderVisitor();
        expressionStringBuilder.Visit(expression);
        return expressionStringBuilder.GetText();
    }


    public static void Shuffle(this string[] array)
    {
        Random rng = new Random();
        int n = array.Length;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            // Swap array[i] with array[j]
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    public static List<string> ChunkText(this string text, int chunkSize = 500, int overlap = 100)
    {
        var chunks = new List<string>();
        int position = 0;

        while (position < text.Length)
        {
            // Calculate the end position for the current chunk
            int end = Math.Min(position + chunkSize, text.Length);

            // Adjust the end position to avoid splitting words (if possible)
            if (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                int lastSpace = text.LastIndexOf(' ', end);
                if (lastSpace > position)
                {
                    end = lastSpace;
                }
            }

            // Extract the chunk and add it to the list
            string chunk = text.Substring(position, end - position).Trim();
            chunks.Add(chunk);

            // Move the position forward, considering the overlap
            position = end - overlap;
        }

        return chunks;
    }
}