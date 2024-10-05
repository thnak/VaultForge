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
}