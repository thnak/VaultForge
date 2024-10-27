using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Utils;

public static class DatabaseExtensions
{
    public static string GetFieldName<T>(this Expression<Func<T, object>> expression)
    {
        if (expression.Body is UnaryExpression unaryExpression)
        {
            // For value types boxed to object
            return ((MemberExpression)unaryExpression.Operand).Member.Name;
        }

        if (expression.Body is MemberExpression memberExpression)
        {
            // For reference types
            return memberExpression.Member.Name;
        }

        throw new InvalidOperationException("Invalid expression");
    }

    public static string GetFieldName<TModel, TProperty>(this Expression<Func<TModel, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression && unaryExpression.Operand is MemberExpression unaryMemberExpression)
        {
            return unaryMemberExpression.Member.Name;
        }

        throw new InvalidOperationException("Invalid expression");
    }
    
    public static ProjectionDefinition<T>? ProjectionBuilder<T>(this Expression<Func<T, object>>[] expression)
    {
        ProjectionDefinition<T>? projection = null;

        if (expression.Length > 0)
        {
            var projectionBuilder = Builders<T>.Projection;
            var bsonProjection = new BsonDocument();

            // Convert the expressions to field names
            foreach (var field in expression)
            {
                var fieldName = field.GetFieldName();
                bsonProjection.Add(fieldName, 1);
            }

            projection = projectionBuilder.Combine(bsonProjection);
        }
        return projection;
    } 
}