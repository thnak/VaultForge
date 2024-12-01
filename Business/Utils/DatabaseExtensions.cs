using System.Linq.Expressions;
using Business.Models;
using BusinessModels.Base;
using BusinessModels.General.Results;
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

    public static ProjectionDefinition<T> ProjectionBuilder<T>(this Expression<Func<T, object>>[] expression)
    {
        ProjectionDefinition<T> projection = new BsonDocumentProjectionDefinition<T>(new BsonDocument());

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

    public static async Task<Result<bool>> UpdateAsync<T>(this IMongoCollection<T> collection, string key, FieldUpdate<T> updates, CancellationToken cancellationToken = default) where T : BaseModelEntry
    {
        if (ObjectId.TryParse(key, out var id))
        {
            var filter = Builders<T>.Filter.Eq(f => f.Id, id);

            // Build the update definition by combining multiple updates
            var updateDefinitionBuilder = Builders<T>.Update;
            var updateDefinitions = new List<UpdateDefinition<T>>();

            if (updates.Any())
            {
                foreach (var update in updates)
                {
                    var fieldName = update.Key;
                    var fieldValue = update.Value;

                    // Add the field-specific update to the list
                    updateDefinitions.Add(updateDefinitionBuilder.Set(fieldName, fieldValue));
                }

                // Combine all update definitions into one
                var combinedUpdate = updateDefinitionBuilder.Combine(updateDefinitions);

                await collection.UpdateOneAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
                return Result<bool>.Success("");
            }
        }

        return Result<bool>.Failure("Invalid key", ErrorType.Validation);
    }

    public static ObjectId DefaultId()
    {
        return new ObjectId("000000000000000000000000");
    }
}