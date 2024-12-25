﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Business.Authenticate.AuthorizationRequirement;
using Business.Authenticate.KeyProvider;
using Business.Authenticate.TokenProvider;
using Business.Business.Interfaces.User;
using Business.Constants.Protector;
using Business.Services.Authenticate.Tracer;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Business.Services.Configure;

public static class AuthenticateService
{
    public static IServiceCollection AddAuthenticateService(this IServiceCollection service)
    {
        service.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        service.AddSingleton<FailedLoginTracker>();
        service.AddSingleton<IJsonWebTokenCertificateProvider, JsonWebTokenCertificateProvider>();
        service.AddSingleton<RsaKeyProvider>();
        service.AddCascadingAuthenticationState();

        ConfigureAuthorizationPolicies(service);
        ConfigureCookiePolicy(service);
        ConfigureApplicationCookie(service);
        ConfigureAuthentication(service);
        ConfigureSession(service);
        ConfigureCors(service);

        return service;
    }

    private static void ConfigureAuthorizationPolicies(IServiceCollection service)
    {
        service.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNamesAndRoles.Over18, builder => builder.Requirements.Add(new OverYearOldRequirement(18)));
            options.AddPolicy(PolicyNamesAndRoles.Over14, builder => builder.Requirements.Add(new OverYearOldRequirement(14)));
            options.AddPolicy(PolicyNamesAndRoles.Over7, builder => builder.Requirements.Add(new OverYearOldRequirement(7)));
        });
    }

    private static void ConfigureCookiePolicy(IServiceCollection service)
    {
        service.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.None;
            options.HttpOnly = HttpOnlyPolicy.Always;
            options.Secure = CookieSecurePolicy.Always; // Ensure cookies are always sent over HTTPS
        });
    }

    private static void ConfigureApplicationCookie(IServiceCollection service)
    {
        service.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = CookieNames.AuthorizeCookie;
            options.Cookie.Domain = CookieNames.Domain;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
    }

    private static void ConfigureAuthentication(IServiceCollection service)
    {
        service.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });
        service.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.SlidingExpiration = true;
                options.LoginPath = PageRoutes.Account.SignIn.Src;
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
            })
            .AddJwtBearer(options =>
            {
                // Access the provider
                var provider = service.BuildServiceProvider().GetRequiredService<IJsonWebTokenCertificateProvider>();

                // Set TokenValidationParameters
                options.TokenValidationParameters = provider.GetTokenValidationParameters();

                // Customize events
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        try
                        {
                            var token = context.SecurityToken as JwtSecurityToken;

                            if (token == null)
                            {
                                throw new SecurityTokenException("Invalid token format");
                            }

                            // Validate the token using JsonWebTokenCertificateProvider
                            var validatedPrincipal = provider.ValidateToken(token.RawData, out _);

                            if (validatedPrincipal == null)
                            {
                                throw new SecurityTokenException("Token validation failed");
                            }

                            // Optional: Add custom claims or additional logic
                            Console.WriteLine("Token validated successfully with JsonWebTokenCertificateProvider.");
                        }
                        catch (Exception ex)
                        {
                            // Log or handle token validation errors
                            Console.WriteLine($"Token validation error: {ex.Message}");
                            context.Fail("Token validation failed");
                        }

                        await Task.CompletedTask;
                    }
                };
            });
    }

    private static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var userPrincipal = context.Principal;
        if (userPrincipal == null)
        {
            await RejectAsync(context);
            return;
        }

        var authenticationType = userPrincipal.Identity?.AuthenticationType ?? string.Empty;
        if (authenticationType == CookieNames.AuthenticationType)
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<IUserBusinessLayer>();
            var userId = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
            var user = userId == null ? null : userManager.Get(userId);
            if (user == null) await RejectAsync(context);
        }
        else
        {
            await RejectAsync(context);
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static void ConfigureSession(IServiceCollection service)
    {
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
    }

    private static void ConfigureCors(IServiceCollection service)
    {
        const string allowAllOriginsPolicy = "AllowAllOrigins";
        service.AddCors(options =>
        {
            options.AddPolicy(allowAllOriginsPolicy, policyBuilder => policyBuilder
                .WithOrigins("localhost:5217", "https://thnakdevserver.ddns.net:5001", "http://34.199.8.144:80", "https://34.199.8.144:80")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials());
        });
    }
}