using Business.Business.Repositories.InternetOfThings;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Business.Utils;

public static class BusinessExtension
{
    public static void AddExtendBusinessService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IoTRequestQueue>();
    }
}