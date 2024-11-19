using System.Runtime.CompilerServices;
using MongoDB.Driver;

namespace Business.Data.Repositories;

public static class DataContextExtensions
{
    public static async IAsyncEnumerable<T> GetAll<T>(this IMongoCollection<T> dataDb, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var filter = Builders<T>.Filter.Empty;
        using var cursor = await dataDb.FindAsync(filter, cancellationToken: cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var model in cursor.Current)
            {
                yield return model;
            }
        }
    }
}