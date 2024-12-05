using Business.Business.Utils;
using Business.Exceptions;
using Business.Services.Configure;
using Business.Services.Http.CircuitBreakers;
using Business.Utils.HttpExtension;
using BusinessModels.Converter;
using BusinessModels.Resources;
using Microsoft.Extensions.Options;

namespace ResApi;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = long.MaxValue;
            options.Limits.MinRequestBodyDataRate = null;
            options.Limits.MaxRequestBufferSize = 1024 * 1024 * 1024;
        });


        #region Configure Setting

        builder.AddAppOptions();

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

        app.MapControllers();
        app.UseExceptionHandler(_ => { });


        app.MapSignalRHubs();

        app.Run();
    }
}