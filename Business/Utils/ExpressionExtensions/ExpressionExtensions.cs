using System.Linq.Expressions;

namespace Business.Utils.ExpressionExtensions;

public static class ExpressionExtensions
{
    public static bool PredicateContainsIdCheck<TModel, TKey>(this Expression<Func<TModel, bool>> predicate, Expression<Func<TModel, TKey>> idSelector)
    {
        var visitor = new IdCheckExpressionVisitor<TModel, TKey>(idSelector);
        visitor.Visit(predicate);
        return visitor.HasIdCheck;
    }
}

public class IdCheckExpressionVisitor<TModel, TKey>(Expression<Func<TModel, TKey>> idSelector) : ExpressionVisitor
{
    public bool HasIdCheck { get; private set; }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Compare the binary expression's left side to the idSelector
        if (node.NodeType == ExpressionType.Equal &&
            ExpressionComparer.AreEqual(node.Left, idSelector.Body))
        {
            HasIdCheck = true;
        }

        return base.VisitBinary(node);
    }
}

public static class ExpressionComparer
{
    public static bool AreEqual(Expression x, Expression y)
    {
        if (x.NodeType != y.NodeType || x.Type != y.Type)
            return false;

        switch (x)
        {
            case MemberExpression mx when y is MemberExpression my:
                return mx.Member == my.Member && AreEqual(mx.Expression, my.Expression);
            // Add other cases for different types of expressions as needed
            default:
                return x.ToString() == y.ToString(); // As a fallback, compare the string representation
        }
    }
}