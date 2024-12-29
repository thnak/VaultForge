using Business.Constants.Protector;
using Business.KeyManagement;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.Configure;

public static class ProtectorServiceCollectionExtensions
{
    public static IServiceCollection AddProtectorService(this IServiceCollection service)
    {
        service.AddAntiforgery(options =>
        {
            options.Cookie = new CookieBuilder
            {
                MaxAge = TimeSpan.FromHours(ProtectorTime.AntiforgeryCookieMaxAge),
                Name = CookieNames.Antiforgery,
                SameSite = SameSiteMode.Unspecified,
                IsEssential = false,
                HttpOnly = false,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
                Domain = CookieNames.Domain
            };
        });

        service.AddControllersWithViews(options => { options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()); });

        service.AddSingleton<IMongoDbXmlKeyProtectorRepository, MongoDbXmlKeyProtectorRepository>();
        service.AddDataProtection()
            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
            })
            .SetApplicationName(CookieNames.Name)
            .SetDefaultKeyLifetime(TimeSpan.FromDays(7));
        service.AddOptions<KeyManagementOptions>()
            .Configure<IServiceScopeFactory>((options, factory) =>
            {
                options.AuthenticatedEncryptorConfiguration = new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                };
                
                options.AutoGenerateKeys = true;
                options.XmlRepository = new MongoDbXmlKeyProtectorRepository(factory);
            });

        service.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.RequireHeaderSymmetry = false;
            options.ForwardLimit = null;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        service.Configure<SecurityStampValidatorOptions>(options => { options.ValidationInterval = TimeSpan.FromMinutes(5); });

        return service;
    }
}