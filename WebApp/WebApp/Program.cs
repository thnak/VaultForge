using Business.Business.Utils;
using Business.Exceptions;
using Business.Services;
using Business.Services.Configure;
using Business.Services.Http.CircuitBreakers;
using Business.SocketHubs;
using BusinessModels.Converter;
using BusinessModels.General.SettingModels;
using BusinessModels.Resources;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.Options;
using Protector.Certificates.Models;
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
            options.Limits.MaxRequestBufferSize = 1024 * 1024 * 1024;
        });

        builder.Services.AddFrontEndService();
        builder.Services.AddFrontEndScopeService();

        #region Http Client for client side

        builder.Services.AddScoped(_ =>
        {
            var httpClient = new HttpClient(new CookieHandler());
            // httpClient.BaseAddress = new Uri(builder.Environment.);
            return httpClient;
        });

        builder.Services.AddScoped(_ =>
        {
            var httpClient = new HttpClient(new CookieHandler());
            httpClient.BaseAddress = new Uri("https://thnakdevserver.ddns.net:5001");
#pragma warning disable ASP0000
            return new BaseHttpClientService(httpClient, builder.Services.BuildServiceProvider());
#pragma warning restore ASP0000
        });

        #endregion

        #region Configure Setting

        builder.Services.Configure<AppCertificate>(builder.Configuration.GetSection("AppCertificate"));
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

        #endregion


        #region Additionnal services

        builder.Services.AddDataServiceCollection();
        builder.Services.AddIotQueueService();
        
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

        StaticCompositeResolver.Instance.Register(
            StandardResolver.Instance,
            NativeDecimalResolver.Instance,
            NativeGuidResolver.Instance,
            NativeDateTimeResolver.Instance,
            MongoObjectIdResolver.INSTANCE);

        builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = int.MaxValue;
                options.MaximumParallelInvocationsPerClient = 100;
            }).AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new ObjectIdConverter()); })
            .AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithResolver(StaticCompositeResolver.Instance)
                    .WithSecurity(MessagePackSecurity.UntrustedData);
            });

        #endregion

        #region Exception Handler

        builder.Services.AddExceptionHandler<ErrorHandling>();

        #endregion

        builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new ObjectIdConverter()); });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<LazyAssemblyLoader>();

        #region Logging

        // builder.Services.AddLogging(options =>
        // {
        //     // options.ClearProviders();
        //     var serviceProvider = builder.Services.BuildServiceProvider();
        //     options.AddProvider(new MongoDbLoggerProvider(serviceProvider.GetRequiredService<IMongoDataLayerContext>()));
        //     options.SetMinimumLevel(LogLevel.Debug);
        // });
        // builder.Services.AddHttpLogging();
        // builder.Logging.SetMinimumLevel(LogLevel.Information);

        #endregion

        var app = builder.Build();
        app.UseStatusCodePagesWithRedirects($"{PageRoutes.Error.Name}/{{0}}");


        #region Localization Setup

        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()!.Value;
        app.UseRequestLocalization(localizationOptions);

        #endregion

        if (app.Environment.IsDevelopment())
        {
            // app.UseWebAssemblyDebugging();
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
        app.MapHub<PageCreatorHub>("/PageCreatorHub");
        app.Run();
    }
}