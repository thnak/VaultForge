using System.Diagnostics.CodeAnalysis;
using Business.Services.RetrievalAugmentedGeneration.Implement;
using Business.Services.RetrievalAugmentedGeneration.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.RetrievalAugmentedGeneration.Utils;

public static class RetrievalAugmentedGenerationExtension
{
    [Experimental("SKEXP0020")]
    public static void AddRetrievalAugmentedGeneration(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IMovieDatabase, MovieDatabase>();

        serviceCollection.AddHostedService<HostApplicationLifetimeEventsHostedService>();
    }
}