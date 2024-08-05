using System.Globalization;
using System.Text.Json;
using Blazored.Toast;
using BlazorWorker.Core;
using BusinessModels.Converter;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using WebApp.Client.Authenticate;
using WebApp.Client.Services.Http;
using WebApp.Client.Services.UserInterfaces;
using WebApp.Client.Utils;

namespace WebApp.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = false;
            config.SnackbarConfiguration.NewestOnTop = false;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 10000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });
        builder.Services.AddBlazoredToast();
        builder.Services.AddWorkerFactory();

        #region Event Service

        builder.Services.AddSingleton<StateContainer>();
        builder.Services.AddSingleton<DocumentObjectModelEventListener>();

        #endregion

        #region Authorize

        builder.Services.AddAuthorizationCore();
        builder.Services.AddAuthenticationStateDeserialization();
        builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddScoped<ProtectedLocalStorage>();
        builder.Services.AddScoped<ProtectedSessionStorage>();
        builder.Services.AddSingleton(new JsonSerializerOptions
        {
            Converters = { new ObjectIdConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });

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