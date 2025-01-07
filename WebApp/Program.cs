using Business.Business.Utils;
using Business.Exceptions;
using Business.Services.Configure;
using Business.Services.Http.CircuitBreakers;
using Business.Utils.HttpExtension;
using BusinessModels.Converter;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using WebApp.Authenticate;
using WebApp.Client.Services;
using WebApp.Client.Services.Http;
using WebApp.Components;
using WebApp.MiddleWares;
using _Imports = WebApp.Client._Imports;

namespace WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment())
            .AddInteractiveWebAssemblyComponents()
            .AddInteractiveServerComponents(options => { options.DetailedErrors = builder.Environment.IsDevelopment(); })
            .AddAuthenticationStateSerialization();

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = long.MaxValue;
            options.Limits.MinRequestBodyDataRate = null;
            options.Limits.MaxRequestBufferSize = long.MaxValue;
        });

        // builder.Services.Configure<FormOptions>(options =>
        // {
        //     options.MultipartBodyLengthLimit = long.MaxValue;
        //     options.MultipartBoundaryLengthLimit = int.MaxValue;
        //     options.ValueLengthLimit = int.MaxValue;
        // });

        builder.Services.AddFrontEndService();
        builder.Services.AddFrontEndScopeService();

        #region Http Client for client side

        builder.Services.AddScoped(_ =>
        {
            var httpClient = new HttpClient(new CookieHandler());
            return httpClient;
        });

        builder.Services.AddScoped<BaseHttpClientService>();

        #endregion

        #region Configure Setting

        builder.AddAppOptions();

        #endregion

        #region Additionnal services

        builder.Services.AddDataServiceCollection();
        builder.Services.AddIotQueueService();
        builder.Services.AddFontStyle();

        #endregion

        #region Caching

        builder.Services.AddCachingService();

        #endregion

        #region Cultures

        builder.Services.AddCultureService();

        #endregion

        #region Authenticate & Protection

        builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
        builder.Services.AddAuthenticateService();
        builder.Services.AddProtectorService();

        #endregion

        #region Rate Limit

        builder.Services.AddRateLimitService();

        #endregion

        #region CurcuitBreaker

        builder.Services.AddCircuitBreaker();

        #endregion

        #region SignalR

        builder.Services.AddSignalRService();

        #endregion

        #region Exception Handler

        builder.Services.AddExceptionHandler<ErrorHandling>();

        #endregion

        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new ObjectIdConverter()); });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<LazyAssemblyLoader>();

        #region Logging

        builder.Services.AddLogger();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        #endregion

        var app = builder.Build();
        app.UseStatusCodePagesWithRedirects($"{PageRoutes.Error.Name}/{{0}}");


        #region Localization Setup

        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()!.Value;
        app.UseRequestLocalization(localizationOptions);

        #endregion

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseResponseCompression();
            app.UseHsts();
        }

        app.UseAntiforgery();

        app.UseCors("AllowAllOrigins");
        app.UseRateLimiter();

        app.UseResponseCaching();
        app.UseOutputCache();

        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();


        app.MapStaticAssets();
        app.MapControllers();
        app.UseMiddleware<Middleware>();
        app.UseExceptionHandler(_ => { });

        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(_Imports).Assembly);

        app.MapSignalRHubs();

        app.Run();
    }
}