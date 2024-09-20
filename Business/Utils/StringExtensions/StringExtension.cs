using System.Linq.Expressions;
using Business.Utils.ExpressionExtensions;

namespace Business.Utils.StringExtensions;

public static class StringExtension
{
    public static string GetCacheKey<T>(this Expression<Func<T, bool>> expression)
    {
        var expressionStringBuilder = new ExpressionStringBuilder();
        return expressionStringBuilder.GetString(expression);
    }
}