using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.Http.CircuitBreakers;

public static class CircuitBreakerExtension
{
    public static void AddCircuitBreaker(this IServiceCollection services)
    {
        services.AddSingleton<IoTCircuitBreakerService>();
    }
}