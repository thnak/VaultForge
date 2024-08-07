using Business.Business.Interfaces.User;
using Business.Business.Repositories.User;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.User;
using Business.Data.Repositories;
using Business.Data.Repositories.FileSystem;
using Business.Data.Repositories.User;
using Business.Models;
using Business.Services;
using BusinessModels.Converter;
using BusinessModels.General;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Protector.Certificates.Models;
using WebApp.Authenticate;
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
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization();

        #region Configure Setting

        builder.Services.Configure<DbSettingModel>(builder.Configuration.GetSection("DBSetting"));
        builder.Services.Configure<AppCertificate>(builder.Configuration.GetSection("AppCertificate"));
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

        #endregion


        #region Additionnal services
        
        builder.Services.AddSingleton<IMongoDataLayerContext, MongoDataLayerContext>();
        builder.Services.AddSingleton<IUserDataLayer, UserDataLayer>();
        builder.Services.AddSingleton<IUserBusinessLayer, UserBusinessLayer>();
        builder.Services.AddSingleton<IFolderSystemDatalayer, FolderSystemDatalayer>();
        builder.Services.AddSingleton<IFileSystemDatalayer, FileSystemDatalayer>();

        builder.Services.AddHostedService<HostApplicationLifetimeEventsHostedService>();

        #endregion

        #region Caching

        if (!builder.Environment.IsDevelopment())
            builder.Services.AddResponseCompression(options =>
            {
                options.MimeTypes = new[]
                {
                    "text/html", "text/css"
                };
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

        builder.Services.AddDistributedMemoryCache(options => { options.ExpirationScanFrequency = TimeSpan.FromSeconds(30); });

        builder.Services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(30),
                LocalCacheExpiration = TimeSpan.FromSeconds(30),
                Flags = HybridCacheEntryFlags.None
            };
        });

        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(TimeSpan.FromSeconds(10)));
            options.DefaultExpirationTimeSpan = OutputCachingPolicy.Expire30;

            options.AddPolicy(nameof(OutputCachingPolicy.Expire10), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire10));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire20), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire20));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire30), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire30));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire40), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire40));

            options.AddPolicy(nameof(OutputCachingPolicy.Expire50), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire50));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire60), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire60));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire120), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire120));
            options.AddPolicy(nameof(OutputCachingPolicy.Expire240), outputCachePolicyBuilder => outputCachePolicyBuilder.Expire(OutputCachingPolicy.Expire240));
        });
        builder.Services.AddResponseCaching();

        #endregion

        #region Cultures

        builder.Services.AddCultureService();

        #endregion

        #region Logging

        builder.Services.AddLogging();
        builder.Services.AddHttpLogging();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        #endregion

        #region Authenticate & Protection

        builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
        builder.Services.AddAuthenticateService();
        builder.Services.AddProtectorService();

        #endregion

        #region Rate Limit

        builder.Services.AddRateLimitService();

        #endregion

        builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new ObjectIdConverter()); });

        var app = builder.Build();

        #region Localization Setup

        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()!.Value;
        app.UseRequestLocalization(localizationOptions);

        #endregion

        app.UseStatusCodePagesWithRedirects(PageRoutes.Error.Default404);


        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseResponseCompression();
            app.UseHsts();
        }

        app.UseHttpsRedirection();
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
        app.UseMiddleware<ErrorHandlingMiddleware>();

        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode(options => options.ServeMultithreadingHeaders = false)
            .AddAdditionalAssemblies(typeof(_Imports).Assembly);
        
        app.Run();
    }
}