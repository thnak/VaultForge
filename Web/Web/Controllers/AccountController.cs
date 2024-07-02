using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using Business.Business.Interfaces.User;
using BusinessModels.People;
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
using Protector.Utils;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController(
    IUserBusinessLayer userBl,
    JsonWebTokenCertificateProvider jsonWebTokenCertificateProvider,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpPost("validate-user")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Sliding)]
    public IActionResult ValidateUser([FromForm] string userName)
    {
        var res = userBl.ValidateUsername(userName);
        return res.Item1 ? Ok() : BadRequest(res.Item2);
    }

    [HttpPost("validate-password")]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Sliding)]
    public IActionResult ValidatePassword([FromForm] string userName, [FromForm] string password)
    {
        var res = userBl.ValidatePassword(userName, password);
        return res.Item1 ? Ok() : BadRequest(res.Item2);
    }


    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    public async Task<IActionResult> AccountSignIn([FromForm] RequestLoginModel request)
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

        if (!string.IsNullOrEmpty(request.ReturnUrl))
            return Redirect(PageRoutes.Account.SignInError.AppendAndEncodeBase64StringAsUri([authenticateState.Item2, request.ReturnUrl]));
        return Redirect(PageRoutes.Account.SignInError.AppendAndEncodeBase64StringAsUri(authenticateState.Item2));
    }




    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    public async Task<IActionResult> AccountSignUp([FromForm] RequestRegisterModel request)
    {
        var userNameHashed = request.Username.ComputeSha256Hash();
        var user = userBl.Get(userNameHashed);
        if (user != null) return Redirect(PageRoutes.Account.SignInError.AppendAndEncodeBase64StringAsUri(AppLang.User_is_already_exists));

        var validateContext = new ValidationContext(request, serviceProvider: null, items: null);
        var validationResults = new List<ValidationResult>();

        bool isValid = Validator.TryValidateObject(request, validateContext, validationResults, true);

        if (isValid)
        {
            var createResult = await userBl.CreateAsync(new UserModel()
            {
                UserName = userNameHashed,
                Password = request.Password.ComputeSha256Hash(),
                BirthDay = request.BirthDay,
                FullName = request.FullName
            });

            if (createResult.Item1) return Redirect("/api/Account/login");
            return Redirect(PageRoutes.Account.SignInError.AppendAndEncodeBase64StringAsUri(createResult.Item2));
        }
        foreach (var result in validationResults)
        {
            if (result.ErrorMessage != null)
                return Redirect(PageRoutes.Account.SignInError.AppendAndEncodeBase64StringAsUri(result.ErrorMessage));
        }
        return Redirect(PageRoutes.Account.SignInError.AppendAndEncodeBase64StringAsUri(AppLang.Registration_failed));
    }

    [HttpGet("signout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AccountSignOut()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect(PageRoutes.Account.SignIn);
    }

    [HttpPost("getJwt")]
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