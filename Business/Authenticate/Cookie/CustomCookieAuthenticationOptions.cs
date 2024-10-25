using System.Security.Claims;
using Business.Authenticate.TokenProvider;
using Business.Business.Interfaces.User;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Protector;

namespace Business.Authenticate.Cookie;

using Microsoft.AspNetCore.Authentication.Cookies;

public class CustomCookieAuthenticationOptions : CookieAuthenticationOptions
{
    public CustomCookieAuthenticationOptions()
    {
        SlidingExpiration = true;
        LoginPath = PageRoutes.Account.SignIn.Src;
        LogoutPath = PageRoutes.Account.Logout;
        AccessDeniedPath = PageRoutes.Account.Denied;
        ExpireTimeSpan = TimeSpan.FromHours(ProtectorTime.CookieExpireTimeSpan);
        Cookie = new CookieBuilder
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
        Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = ValidateAsync
        };
    }

    private static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var userPrincipal = context.Principal;
        if (userPrincipal == null)
        {
            await Reject(context);
            return;
        }
        var authenticationType = userPrincipal.Identity?.AuthenticationType ?? string.Empty;
        if (authenticationType == CookieNames.AuthenticationType)
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<IUserBusinessLayer>();
            var jswProvider = context.HttpContext.RequestServices.GetRequiredService<IJsonWebTokenCertificateProvider>();
            var jwt = userPrincipal.FindFirst(ClaimTypes.UserData)?.Value;
            if (!string.IsNullOrEmpty(jwt))
            {
                var claimsPrincipal = jswProvider.GetClaimsFromToken(jwt);
                if (claimsPrincipal == null)
                {
                    await Reject(context);
                    return;
                }
            }

            var userId = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
            var user = userId == null ? null : userManager.Get(userId);
            if (user == null) await Reject(context);
        }
        else
        {
            await Reject(context);
        }
    }

    private static async Task Reject(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}