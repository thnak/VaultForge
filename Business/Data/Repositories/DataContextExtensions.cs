using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Models;
using Business.Utils;
using BusinessModels.Base;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Business.Data.Repositories;

public static class DataContextExtensions
{
    public static async IAsyncEnumerable<T> GetAll<T>(this IMongoCollection<T> dataDb, Expression<Func<T, object>>[] field2Fetch, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = Builders<T>.Filter.Empty;
        using var cursor = await dataDb.FindAsync(filter, new FindOptions<T, T>()
        {
            Projection = field2Fetch.ProjectionBuilder()
        }, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }

    public static T? Get<T>(this IMongoCollection<T> dataDb, string key) where T : BaseModelEntry
    {
        if (ObjectId.TryParse(key, out var objectId))
        {
            return dataDb.Find(x => x.Id == objectId).FirstOrDefault();
        }

        return null;
    }

    public static async Task<Result<T?>> Get<T>(this IMongoCollection<T> dataDb, string key, params Expression<Func<T, object>>[] fieldsToFetch) where T : BaseModelEntry
    {
        if (ObjectId.TryParse(key, out ObjectId objectId))
        {
            var findOptions = fieldsToFetch.Any() ? new FindOptions<T, T>() { Projection = fieldsToFetch.ProjectionBuilder(), Limit = 1 } : null;
            using var cursor = await dataDb.FindAsync(x => x.Id == objectId, findOptions);
            var fileModel = cursor.FirstOrDefault();
            if (fileModel != null) return Result<T?>.Success(fileModel);
            return Result<T?>.Failure(AppLang.Article_does_not_exist, ErrorType.NotFound);
        }

        return Result<T?>.Failure(AppLang.Invalid_key, ErrorType.Validation);
    }

    public static IEnumerable<T?> Get<T>(this IMongoCollection<T> dataDb, List<string> keys, CancellationToken cancellationToken = default) where T : BaseModelEntry
    {
        foreach (var key in keys.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
        {
            yield return dataDb.Get(key);
        }
    }

    public static async Task<long> GetDocumentSizeAsync<T>(this IMongoCollection<T> dataDb, CancellationToken cancellationToken = default)
    {
        var result = await dataDb.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);
        return result;
    }

    public static async Task<long> GetDocumentSizeAsync<T>(this IMongoCollection<T> dataDb, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var result = await dataDb.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
        return result;
    }

    public static async Task<Result<string>> UpdateAsync<T>(this IMongoCollection<T> dataDb, string key, FieldUpdate<T> updates, CancellationToken cancellationToken = default) where T : BaseModelEntry
    {
        try
        {
            var oldValue = dataDb.Get(key);
            if (oldValue == null) return Result<string>.Failure(AppLang.Invalid_key, ErrorType.NotFound);

            var filter = Builders<T>.Filter.Eq(f => f.Id, oldValue.Id);

            // Build the update definition by combining multiple updates
            var updateDefinitionBuilder = Builders<T>.Update;
            var updateDefinitions = new List<UpdateDefinition<T>>();

            if (updates.Any())
            {
                updates.Add(x => x.ModifiedTime, DateTime.UtcNow);
                foreach (var update in updates)
                {
                    var fieldName = update.Key;
                    var fieldValue = update.Value;

                    // Add the field-specific update to the list
                    updateDefinitions.Add(updateDefinitionBuilder.Set(fieldName, fieldValue));
                }

                // Combine all update definitions into one
                var combinedUpdate = updateDefinitionBuilder.Combine(updateDefinitions);

                await dataDb.UpdateOneAsync(filter, combinedUpdate, cancellationToken: cancellationToken);
            }

            return Result<string>.Success(AppLang.Update_successfully);
        }
        catch (OperationCanceledException)
        {
            return Result<string>.Failure(AppLang.Cancel, ErrorType.Cancelled);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure(ex.Message, ErrorType.Validation);
        }
    }
}