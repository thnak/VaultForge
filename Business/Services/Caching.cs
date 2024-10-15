using Business.Models;

#if DEBUG
#else
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
#endif


using Microsoft.Extensions.DependencyInjection;

namespace Business.Services;

public static class Caching
{
    public static IServiceCollection AddCachingService(this IServiceCollection service)
    {
#if DEBUG
#else
        service.AddResponseCompression(options =>
        {
            options.MimeTypes = new[]
            {
                "text/html", "text/css"
            };
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
#endif

        service.AddDistributedMemoryCache(options => { options.ExpirationScanFrequency = TimeSpan.FromSeconds(30); });

        // service.AddHybridCache(options =>
        // {
        //     options.DefaultEntryOptions = new HybridCacheEntryOptions
        //     {
        //         Expiration = TimeSpan.FromSeconds(30),
        //         LocalCacheExpiration = TimeSpan.FromSeconds(30),
        //         Flags = HybridCacheEntryFlags.None
        //     };
        // });

        service.AddOutputCache(options =>
        {
            options.AddBasePolicy(outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(TimeSpan.FromSeconds(10)));
            options.DefaultExpirationTimeSpan = OutputCachingPolicy.Expire30;

            options.AddPolicy(nameof(OutputCachingPolicy.Expire10), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire10));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire20), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire20));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire30), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire30));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire40), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire40));

            options.AddPolicy(nameof(OutputCachingPolicy.Expire50), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire50));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire60), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire60));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire120), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire120));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire240), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire240));
        });
        service.AddResponseCaching();
        return service;
    }
}