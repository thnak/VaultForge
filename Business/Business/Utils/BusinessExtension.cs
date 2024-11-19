using System.Collections.Concurrent;
using BrainNet.Database;
using BrainNet.Models.Result;
using BrainNet.Models.Vector;
using Business.Business.Interfaces.InternetOfThings;
using Business.Business.Repositories.InternetOfThings;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Business.Utils;

public static class BusinessExtension
{
    public static void AddIotQueueService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IIotRequestQueue, IotRequestQueue>();
        serviceCollection.AddHostedService<IoTRequestQueueHostedService>();
    }

    public static async Task<List<SearchScore<VectorRecord>>> RagSearch(this ConcurrentDictionary<string, IVectorDb> vectorDictionary, string query, int count, CancellationToken cancellationToken = default)
    {
        List<SearchScore<VectorRecord>> result = [];
        foreach (var collectionPair in vectorDictionary)
        {
            var vectorSearch = await collectionPair.Value.GenerateVectorsFromDescription(query, cancellationToken);

            await foreach (var co in collectionPair.Value.Search(vectorSearch, count, cancellationToken))
            {
                result.Add(co);
            }
        }

        result = [..result.OrderBy(x => x.Score).Take(count)];
        return result;
    }

    public static async Task<List<SearchScore<VectorRecord>>> RagSearch(this IVectorDb vectorDb, string query, int count, CancellationToken cancellationToken = default)
    {
        List<SearchScore<VectorRecord>> result = [];
        var vectorSearch = await vectorDb.GenerateVectorsFromDescription(query, cancellationToken);


        await foreach (var co in vectorDb.Search(vectorSearch, count, cancellationToken))
        {
            result.Add(co);
        }


        result = [..result.OrderBy(x => x.Score).Take(count)];
        return result;
    }
}