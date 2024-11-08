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
}