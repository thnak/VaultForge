using Business.Business.Interfaces.User;
using Business.Business.Repositories.User;
using Business.Data.Interfaces;
using Business.Data.Interfaces.FileSystem;
using Business.Data.Interfaces.User;
using Business.Data.Repositories;
using Business.Data.Repositories.FileSystem;
using Business.Data.Repositories.User;
using Business.Services;
using BusinessModels.Converter;
using BusinessModels.General;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components.Authorization;
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
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddInteractiveServerComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment())
            .AddAuthenticationStateSerialization();
        
        
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

        builder.Services.AddCachingService();

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
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(_Imports).Assembly);
        
        app.Run();
    }
}