using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
using Business.Data.Repositories;
using Business.Data.Repositories.User;
using BusinessModels.General;
using Microsoft.Extensions.Caching.Hybrid;
using ResApi.Services;

namespace ResApi;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.Configure<DbSettingModel>(builder.Configuration.GetSection("DBSetting"));
        builder.Services.AddScoped<IMongoDataLayerContext, MongoDataLayerContext>();
        builder.Services.AddScoped<IUserDataLayer, UserDataLayer>();

        builder.Services.AddHostedService<StartupService>();

        builder.Services.AddResponseCompression();
        builder.Services.AddHybridCache(options => {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(30),
                LocalCacheExpiration = TimeSpan.FromSeconds(30),
                Flags = HybridCacheEntryFlags.None
            };
        });
        builder.Services.AddOutputCache(options => { options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(30); });
        builder.Services.AddResponseCaching();


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseResponseCompression();
        app.UseResponseCaching();
        app.UseOutputCache();

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}