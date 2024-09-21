using System.Linq.Expressions;
using System.Text;
using BusinessModels.Utils;

namespace Business.Utils.ExpressionExtensions;

public class ExpressionStringBuilderVisitor : ExpressionVisitor
{
    public string GetText() => _stringBuilder.ToString();
    private readonly StringBuilder _stringBuilder = new();

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var propertyName = GetMemberName(node.Left);
        var propertyValue = GetValue(node.Right);

        _stringBuilder.Append(propertyName);
        _stringBuilder.Append(node.NodeType.ToString());
        _stringBuilder.Append(propertyValue?.ToJson());

        return base.VisitBinary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var collection = node.Object != null ? GetValue(node.Object) : null;
        var propertyName = node.Arguments.Select(GetMemberName).ToArray().ToJson();

        _stringBuilder.Append(propertyName);
        _stringBuilder.Append(node.Method.Name);
        _stringBuilder.Append(collection?.ToJson());

        return base.VisitMethodCall(node);
    }

    private string? GetMemberName(Expression expression)
    {
        if (expression is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        return null;
    }

    private object? GetValue(Expression expression)
    {
        if (expression is ConstantExpression constantExpression)
        {
            return constantExpression.Value;
        }

        if (expression is not MemberExpression memberExpression) return null;
        // Handling captured variables (e.g., contentFolderTypesList)
        if (memberExpression.Expression is not ConstantExpression constantExpression2) return null;
        var container = constantExpression2.Value;
        var field = container?.GetType().GetField(memberExpression.Member.Name);
        var property = container?.GetType().GetProperty(memberExpression.Member.Name);
        return field?.GetValue(container) ?? property?.GetValue(container);
    }
}