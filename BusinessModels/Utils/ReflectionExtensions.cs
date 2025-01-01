using System.Linq.Expressions;

namespace BusinessModels.Utils;

public static class ReflectionExtensions
{
    public static string GetMemberName<T>(this Expression<Func<T, object>> expression) where T : class
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operandMember)
        {
            return operandMember.Member.Name;
        }

        throw new ArgumentException("Invalid expression");
    }

    public static IEnumerable<string> GetMemberNames<T>(this T _, params Expression<Func<T, object>>[] expressions) where T : class
    {
        foreach (var expression in expressions)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                yield return memberExpression.Member.Name;
            }
            else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression operandMember)
            {
                yield return operandMember.Member.Name;
            }
            else
            {
                throw new ArgumentException("Invalid expression");
            }
        }
    }
}