using System.Net.Mime;
using System.Security.Claims;
using Business.Business.Interfaces.User;
using BusinessModels.Resources;
using BusinessModels.Secure;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Protector.Certificates;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController(
    IUserBusinessLayer userBl,
    JsonWebTokenCertificateProvider jsonWebTokenCertificateProvider,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    public async Task<IActionResult> Login([FromForm] RequestLoginModel request)
    {
        var authenticateState = userBl.Authenticate(request);
        if (authenticateState.Item1)
        {
            var claimsIdentity = userBl.CreateIdentity(request.UserName);
            var claimPrincipal = new ClaimsPrincipal(claimsIdentity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal);
            if (string.IsNullOrEmpty(request.ReturnUrl))
            {
                return Redirect("/");
            }
            return Redirect($"/{request.ReturnUrl}");
        }

        return Redirect(PageRoutes.Account.LoginError.AppendAndEncodeBase64StringAsUri(authenticateState.Item2));
    }

    [HttpPost("GetJwt")]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    [ValidateAntiForgeryToken]
    public IActionResult GetJwt([FromForm] RequestLoginModel request)
    {
        var authenticateState = userBl.Authenticate(request);
        if (authenticateState.Item1)
        {
            var claims = userBl.GetAllClaim(request.UserName);
            var token = jsonWebTokenCertificateProvider.GenerateJwtToken(claims);
            return Content(token, MediaTypeNames.Application.Json);
        }
        return Ok();
    }

    [HttpGet("GetWeatwqwqher")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{CookieAuthenticationDefaults.AuthenticationScheme}")]
    // [DisableFormValueModelBinding]
    public IActionResult GetWeatwqwqher()
    {
        var con = HttpContext;
        return Ok(AppLang.Hello);
    }
    
    [HttpPost("GetWeather2")]
    [ValidateAntiForgeryToken]
    public IActionResult GetWeather2()
    {
        return Ok(AppLang.Hello);
    }
    
    
    /// <summary>
    /// Manual get AntiForgeryToken for your self. it's also add a cookie for you
    /// </summary>
    /// <param name="cookieName">Optional support for multiple request</param>
    /// <returns></returns>
    [HttpGet("GetAntiForgeryToken")]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    public IActionResult GetAntiForgeryToken(string? cookieName)
    {
        var isNullOrEmpty = string.IsNullOrEmpty(cookieName);
        var tokens = isNullOrEmpty ? antiforgery.GetAndStoreTokens(HttpContext) : antiforgery.GetTokens(HttpContext);
        if (!isNullOrEmpty)
        {
            if (tokens.RequestToken != null) HttpContext.Response.Cookies.Append(cookieName!, tokens.RequestToken);
        }
        return new JsonResult(new
        {
            token = tokens.RequestToken
        });
    }
}