using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Business.Utils;
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
}