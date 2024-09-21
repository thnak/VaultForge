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
}