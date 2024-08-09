using System.Globalization;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using WebApp.Client.Authenticate;
using WebApp.Client.Services;
using WebApp.Client.Services.Http;
using WebApp.Client.Utils;

namespace WebApp.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddFrontEndService();
        builder.Services.AddFrontEndSingletonService();


        #region Authorize

        builder.Services.AddAuthorizationCore();
        builder.Services.AddAuthenticationStateDeserialization();
        builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
        builder.Services.AddCascadingAuthenticationState();

        #endregion

        #region Http Client

        builder.Services.AddScoped(_ =>
        {
            var httpClient = new HttpClient(new CookieHandler());
            httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
            return httpClient;
        });

        builder.Services.AddScoped(_ =>
        {
            var httpClient = new HttpClient(new CookieHandler());
            httpClient.BaseAddress = new Uri("https://thnakdevserver.ddns.net:5001");
            return new BaseHttpClientService(httpClient, builder.Services.BuildServiceProvider());
        });

        #endregion

        builder.Services.AddLocalization();

        var host = builder.Build();

        var defaultCulture = AllowedCulture.SupportedCultures.Select(x => x.Name).ToArray().First();

        var js = host.Services.GetRequiredService<IJSRuntime>();
        var result = await js.GetCulture();
        var culture = CultureInfo.GetCultureInfo(result ?? defaultCulture);

        if (result == null) await js.SetCulture(defaultCulture);

        Thread.CurrentThread.CurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        await host.RunAsync();
    }
}