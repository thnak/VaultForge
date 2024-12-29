using BrainNet.Service.Font.Implements;
using BrainNet.Service.Font.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.Configure;

public static class StyleListServiceCollectionExtensions
{
    public static void AddFontStyle(this IServiceCollection services)
    {
        services.AddSingleton<IFontServiceProvider, FontServiceProvider>();
    }
}