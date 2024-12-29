using System.Threading.RateLimiting;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.Configure;

public static class RateLimitServiceCollectionExtensions
{
    public static IServiceCollection AddRateLimitService(this IServiceCollection service)
    {
        service.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(PolicyNamesAndRoles.LimitRate.Fixed, opt =>
            {
                opt.Window = TimeSpan.FromSeconds(10);
                opt.PermitLimit = 4;
                opt.QueueLimit = 2;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.RejectionStatusCode = 429;
        });
        service.AddRateLimiter(options =>
        {
            options.AddSlidingWindowLimiter(PolicyNamesAndRoles.LimitRate.Sliding, opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(30);
                opt.SegmentsPerWindow = 3;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });

            options.RejectionStatusCode = 429;
        });

        service.AddRateLimiter(options =>
        {
            options.AddTokenBucketLimiter(PolicyNamesAndRoles.LimitRate.Token, opt =>
            {
                opt.TokenLimit = 100;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
                opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
                opt.TokensPerPeriod = 10; //Rate at which you want to fill
                opt.AutoReplenishment = true;
            });

            options.RejectionStatusCode = 429;
        });

        service.AddRateLimiter(options =>
        {
            options.AddConcurrencyLimiter(PolicyNamesAndRoles.LimitRate.Concurrency, opt =>
            {
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
                opt.PermitLimit = 100;
            });

            options.RejectionStatusCode = 429;
        });
        return service;
    }
}