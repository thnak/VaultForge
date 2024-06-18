using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Blazored.Toast;
using Business.Business.Interfaces.User;
using Business.Business.Repositories.User;
using Business.Data.Interfaces;
using Business.Data.Interfaces.User;
using Business.Data.Repositories;
using Business.Data.Repositories.User;
using BusinessModels.General;
using BusinessModels.Resources;
using CodeWithMe.Authenticate;
using CodeWithMe.Components;
using CodeWithMe.MiddleWares;
using CodeWithMe.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using Protector;
using Protector.Certificates;
using Protector.Certificates.Models;
using Protector.KeyProvider;

namespace CodeWithMe
{
    public abstract class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents()
                .AddAuthenticationStateSerialization();

            builder.Services.AddMudServices();
            builder.Services.AddBlazoredToast();

            #region Additionnal services

            builder.Services.Configure<DbSettingModel>(builder.Configuration.GetSection("DBSetting"));
            builder.Services.AddScoped<IMongoDataLayerContext, MongoDataLayerContext>();
            builder.Services.AddScoped<IUserDataLayer, UserDataLayer>();
            builder.Services.AddScoped<IUserBusinessLayer, UserBusinessLayer>();


            builder.Services.AddHostedService<StartupService>();
            builder.Services.AddHostedService<HostApplicationLifetimeEventsHostedService>();
            #endregion

            #region Caching

            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddResponseCompression(options => {
                    options.MimeTypes = new[]
                    {
                        "text/html", "text/css",
                    };
                    options.Providers.Add<BrotliCompressionProvider>();
                    options.Providers.Add<GzipCompressionProvider>();
                });
            }
            
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddHybridCache(options => {
                options.DefaultEntryOptions = new HybridCacheEntryOptions()
                {
                    Expiration = TimeSpan.FromSeconds(30),
                    LocalCacheExpiration = TimeSpan.FromSeconds(30),
                    Flags = HybridCacheEntryFlags.None
                };
            });

            builder.Services.AddOutputCache(options => { options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(30); });
            builder.Services.AddResponseCaching();

            #endregion

            #region Cultures

            builder.Services.AddLocalization();
            builder.Services.Configure<RequestLocalizationOptions>(options => {
                var supportedCultures = AllowedCulture.SupportedCultures.ToArray();
                foreach (var culture in supportedCultures)
                {
                    culture.NumberFormat = NumberFormatInfo.InvariantInfo;
                    culture.DateTimeFormat = DateTimeFormatInfo.InvariantInfo;
                }
                options.SetDefaultCulture(supportedCultures[0].Name);
                options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                options.ApplyCurrentCultureToResponseHeaders = true;
                options.RequestCultureProviders = new List<IRequestCultureProvider>()
                {
                    new CookieRequestCultureProvider()
                    {
                        CookieName = CookieNames.Culture,
                        Options = new RequestLocalizationOptions()
                    },
                };
            });

            #endregion

            #region Authenticate & Protection
            
            builder.Services.AddScoped<AuthenticationStateProvider, PersistingServerAuthenticationStateProvider>();
            builder.Services.AddSingleton<JsonWebTokenCertificateProvider>();
            builder.Services.AddSingleton<RsaKeyProvider>(); 
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(options => {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultForbidScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddCookie(options => {
                    options.SlidingExpiration = true;
                    options.LoginPath = PageRoutes.Account.Login;
                    options.LogoutPath = PageRoutes.Account.Logout;
                    options.AccessDeniedPath = PageRoutes.Account.Denied;
                    options.ExpireTimeSpan = TimeSpan.FromHours(ProtectorTime.CookieExpireTimeSpan);
                    options.Cookie = new CookieBuilder
                    {
                        MaxAge = TimeSpan.FromHours(ProtectorTime.CookieMaxAge),
                        Name = CookieNames.AuthorizeCookie,
                        SameSite = SameSiteMode.Strict,
                        IsEssential = true,
                        HttpOnly = true,
                        SecurePolicy = CookieSecurePolicy.SameAsRequest
                    };
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = ValidateAsync
                    };
                })
                .AddJwtBearer(options => {
                    var cert = builder.Configuration.GetSection(nameof(AppCertificate)).Get<AppCertificate>()!;
                    var certificate = new X509Certificate2(cert.FilePath, cert.Password);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =  new X509SecurityKey(certificate),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Request.Cookies.TryGetValue(CookieNames.JwtTokenName, out string? token);
                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromHours(ProtectorTime.SessionIdleTimeout);
                options.Cookie = new CookieBuilder
                {
                    MaxAge = TimeSpan.FromHours(ProtectorTime.SessionCookieMaxAge),
                    Name = CookieNames.Session,
                    SameSite = SameSiteMode.Strict,
                    Expiration = TimeSpan.FromHours(ProtectorTime.SessionCookieMaxAge),
                    IsEssential = false,
                    HttpOnly = true,
                    SecurePolicy = CookieSecurePolicy.SameAsRequest
                };
            });
            
            builder.Services.AddCors();
            builder.Services.AddAntiforgery(options => {
                options.Cookie = new CookieBuilder
                {
                    MaxAge = TimeSpan.FromHours(ProtectorTime.AntiforgeryCookieMaxAge),
                    Name = CookieNames.Antiforgery,
                    SameSite = SameSiteMode.Strict,
                    IsEssential = true,
                    HttpOnly = true,
                    SecurePolicy = CookieSecurePolicy.SameAsRequest
                };
                // options.HeaderName = "X-XSRF-TOKEN";
            });
            
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });
            
            builder.Services.AddDataProtection()
                .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_GCM,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA512
                })
                .SetApplicationName(CookieNames.Name)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(7));
            
            builder.Services.Configure<ForwardedHeadersOptions>(options => {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.RequireHeaderSymmetry = false;
                options.ForwardLimit = null;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.Configure<SecurityStampValidatorOptions>(options => { options.ValidationInterval = TimeSpan.FromMinutes(5); });

            #endregion

            builder.Services.AddControllers();

            var app = builder.Build();

            #region Localization Setup

            var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()!.Value;
            app.UseRequestLocalization(localizationOptions);

            #endregion

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseResponseCompression();
            }

            app.UseCors();
            app.UseResponseCaching();
            app.UseOutputCache();

            app.UseSession();

            app.UseStaticFiles();
            app.UseAntiforgery();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapStaticAssets();
            app.MapControllers();
            app.MapRazorComponents<App>()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly)
                .AddInteractiveWebAssemblyRenderMode();

            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
            
            app.Run();
        }

        private static async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var userPrincipal = context.Principal;
            var authenticationType = userPrincipal?.Identity?.AuthenticationType ?? string.Empty;
            if (authenticationType == CookieNames.AuthenticationType)
            {
                // Example: Check if the user's security stamp is still valid
                var userManager = context.HttpContext.RequestServices.GetRequiredService<IUserBusinessLayer>();
                var userId = userPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = userId;

                if (user == null)
                {
                    // If the user is not found or is not in the required role, reject the principal
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
            else
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}