using System.Security.Claims;
using Business.Authenticate.AuthorizationRequirement;
using Business.Authenticate.TokenProvider;
using Business.Business.Interfaces.User;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Protector;
using Protector.KeyProvider;
using Protector.Tracer;

namespace Business.Services;

public static class Authenticate
{
    public static IServiceCollection AddAuthenticateService(this IServiceCollection service)
    {
        service.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        service.AddSingleton<FailedLoginTracker>();
        service.AddSingleton<IJsonWebTokenCertificateProvider, JsonWebTokenCertificateProvider>();
        service.AddSingleton<RsaKeyProvider>();
        service.AddCascadingAuthenticationState();
        service.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNamesAndRoles.Over18, policyBuilder => policyBuilder.Requirements.Add(new OverYearOldRequirement(18)));
            options.AddPolicy(PolicyNamesAndRoles.Over14, policyBuilder => policyBuilder.Requirements.Add(new OverYearOldRequirement(14)));
            options.AddPolicy(PolicyNamesAndRoles.Over7, policyBuilder => policyBuilder.Requirements.Add(new OverYearOldRequirement(7)));
        });

        service.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.None;
            options.HttpOnly = HttpOnlyPolicy.Always;
            options.Secure = CookieSecurePolicy.Always; // Ensure cookies are always sent over HTTPS
        });

        service.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = CookieNames.AuthorizeCookie;
            options.Cookie.Domain = CookieNames.Domain;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        service.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.SlidingExpiration = true;
                options.LoginPath = PageRoutes.Account.SignIn;
                options.LogoutPath = PageRoutes.Account.Logout;
                options.AccessDeniedPath = PageRoutes.Account.Denied;
                options.ExpireTimeSpan = TimeSpan.FromHours(ProtectorTime.CookieExpireTimeSpan);
                options.Cookie = new CookieBuilder
                {
                    MaxAge = TimeSpan.FromHours(ProtectorTime.CookieMaxAge),
                    Name = CookieNames.AuthorizeCookie,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    HttpOnly = true,
                    SecurePolicy = CookieSecurePolicy.Always,
                    Domain = CookieNames.Domain,
                    Path = "/"
                };
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = ValidateAsync
                };

                #region Cookie Event Handler

                async Task ValidateAsync(CookieValidatePrincipalContext context)
                {
                    var userPrincipal = context.Principal;
                    if (userPrincipal == null)
                    {
                        await Reject();
                        return;
                    }

                    var authenticationType = userPrincipal.Identity?.AuthenticationType ?? string.Empty;
                    if (authenticationType == CookieNames.AuthenticationType)
                    {
                        // Example: Check if the user's security stamp is still valid
                        var userManager = context.HttpContext.RequestServices.GetRequiredService<IUserBusinessLayer>();
                        var jswProvider = context.HttpContext.RequestServices.GetRequiredService<IJsonWebTokenCertificateProvider>();

                        var jwt = userPrincipal.FindFirst(ClaimTypes.UserData)?.Value;
                        if (!string.IsNullOrEmpty(jwt))
                        {
                            var claimsPrincipal = jswProvider.GetClaimsFromToken(jwt);
                            if (claimsPrincipal == null)
                            {
                                await Reject();
                                return;
                            }
                        }


                        var userId = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
                        var user = userId == null ? null : userManager.Get(userId);
                        if (user == null) await Reject();
                    }
                    else
                    {
                        await Reject();
                    }

                    return;

                    async Task Reject()
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }

                #endregion
            });

        service.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(ProtectorTime.SessionIdleTimeout);
            options.Cookie = new CookieBuilder
            {
                MaxAge = TimeSpan.FromHours(ProtectorTime.SessionCookieMaxAge),
                Name = CookieNames.Session,
                SameSite = SameSiteMode.Strict,
                Expiration = TimeSpan.FromHours(ProtectorTime.SessionCookieMaxAge),
                IsEssential = true,
                HttpOnly = true,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
                Domain = CookieNames.Domain
            };
        });

        service.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                policyBuilder => policyBuilder
                    .WithOrigins("localhost:5217", "https://thnakdevserver.ddns.net:5001")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });
        return service;
    }
}