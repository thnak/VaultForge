using System.Collections.Concurrent;
using BrainNet.Database;
using BrainNet.Models.Result;
using BrainNet.Models.Vector;
using BrainNet.Service.Font.Implements;
using BrainNet.Service.Font.Interfaces;
using Business.Business.Interfaces.InternetOfThings;
using Business.Business.Repositories.InternetOfThings;
using Business.Services.HostedServices.Face;
using Business.Services.HostedServices.IoT;
using Business.Services.OnnxService.Face;
using Business.Services.OnnxService.WaterMeter;
using Business.Services.TaskQueueServices;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Business.Utils;

public static class BusinessExtension
{
    public static void AddIotQueueService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IIotRequestQueue, IotRequestQueue>();
        serviceCollection.AddSingleton<IWaterMeterReaderQueue, WaterMeterReaderQueue>();
        serviceCollection.AddSingleton<IWaterMeterInferenceService, WaterMeterInferenceService>();
        serviceCollection.AddSingleton<IFaceEmbeddingInferenceService, FaceEmbeddingInferenceService>();
        serviceCollection.AddSingleton<IYoloSessionManager, YoloSessionManager>();
        serviceCollection.AddSingleton<IFontServiceProvider, FontServiceProvider>();
        serviceCollection.AddHostedService<IoTRequestQueueBackgroundService>();
        serviceCollection.AddHostedService<WaterMeterInferenceHostedService>();
        serviceCollection.AddHostedService<FaceEmbeddingInferenceHostedService>();
        serviceCollection.AddHostedService<YoloSessionManagerHostedService>();
    }

    public static async Task<List<SearchScore<VectorRecord>>> RagSearch(this ConcurrentDictionary<string, IInMemoryVectorDb> vectorDictionary, string query, int count, CancellationToken cancellationToken = default)
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

    public static async Task<List<SearchScore<VectorRecord>>> RagSearch(this IInMemoryVectorDb iInMemoryVectorDb, string query, int count, CancellationToken cancellationToken = default)
    {
        List<SearchScore<VectorRecord>> result = [];
        var vectorSearch = await iInMemoryVectorDb.GenerateVectorsFromDescription(query, cancellationToken);
        await foreach (var co in iInMemoryVectorDb.Search(vectorSearch, count, cancellationToken))
        {
            result.Add(co);
        }


        result = [..result.OrderByDescending(x => x.Score).Take(count)];
        return result;
    }
}