using System.Linq.Expressions;
using System.Text;

namespace Business.Utils.ExpressionExtensions;

public class ExpressionStringBuilder : ExpressionVisitor
{
    private readonly StringBuilder _stringBuilder = new();

    public string GetString(Expression expression)
    {
        Visit(expression);
        return _stringBuilder.ToString();
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _stringBuilder.Append("(");
        Visit(node.Left);
        _stringBuilder.Append($" {GetOperator(node.NodeType)} "); // Get operator (like ==, &&, etc.)
        Visit(node.Right);
        _stringBuilder.Append(")");
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value == null)
        {
            _stringBuilder.Append("null");
        }
        else if (node.Type == typeof(string))
        {
            _stringBuilder.Append($"\"{node.Value}\"");
        }
        else if (node.Type.IsValueType)
        {
            _stringBuilder.Append(node.Value);
        }

        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // If the member is a constant or field, extract its value
        if (node.Expression is ConstantExpression constantExpression)
        {
            var memberValue = GetValueFromConstantExpression(constantExpression, node.Member.Name);
            _stringBuilder.Append(memberValue);
        }
        else
        {
            _stringBuilder.Append(node.Member.Name);
        }

        return node;
    }

    private object GetValueFromConstantExpression(ConstantExpression constantExpression, string memberName)
    {
        var container = constantExpression.Value;
        var field = container?.GetType().GetField(memberName);
        return field?.GetValue(container) ?? memberName;
    }

    private string GetOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "==",
            ExpressionType.AndAlso => "&&",
            ExpressionType.OrElse => "||",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.NotEqual => "!=",
            _ => nodeType.ToString() // Fallback to the default enum string if operator not found
        };
    }
}