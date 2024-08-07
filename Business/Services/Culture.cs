using System.Globalization;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services;

public static class Culture
{
    public static IServiceCollection AddCultureService(this IServiceCollection service)
    {
        service.AddLocalization();
        service.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = AllowedCulture.SupportedCultures.ToArray();
            foreach (var culture in supportedCultures)
            {
                culture.NumberFormat = NumberFormatInfo.InvariantInfo;
                culture.DateTimeFormat = DateTimeFormatInfo.InvariantInfo;
            }

            options.SetDefaultCulture(supportedCultures[0].Name);
            options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.ApplyCurrentCultureToResponseHeaders = true;
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                new CookieRequestCultureProvider
                {
                    CookieName = CookieNames.Culture,
                    Options = new RequestLocalizationOptions()
                },
                new QueryStringRequestCultureProvider
                {
                    QueryStringKey = CookieNames.Culture,
                    UIQueryStringKey = $"{CookieNames.Culture}-UI"
                },
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });
        return service;
    }
}