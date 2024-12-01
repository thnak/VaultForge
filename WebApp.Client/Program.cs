using System.Globalization;
using BlazorWorker.Core;
using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
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
        builder.Services.AddWorkerFactory();
        ClientSignalRHubExtensions.RegisterResolvers();

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
            httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
            return new BaseHttpClientService(httpClient, builder.Services.BuildServiceProvider());
        });

        #endregion

        builder.Services.AddLocalization();
        builder.Services.AddScoped<LazyAssemblyLoader>();
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