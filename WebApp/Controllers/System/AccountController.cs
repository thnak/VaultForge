using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Security.Claims;
using Business.Authenticate.TokenProvider;
using Business.Business.Interfaces.User;
using Business.Utils.Protector;
using BusinessModels.People;
using BusinessModels.Resources;
using BusinessModels.Secure;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Protector;

namespace WebApp.Controllers.System;

[Route("api/[controller]")]
[ApiController]
public class AccountController(
    IUserBusinessLayer userBl,
    IJsonWebTokenCertificateProvider jsonWebTokenCertificateProvider,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpPost("validate-user")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Sliding)]
    public IActionResult ValidateUser([FromForm] string userName)
    {
        var res = userBl.ValidateUsername(userName);
        return res.Item1 ? Ok() : BadRequest(res.Item2);
    }

    [HttpPost("validate-password")]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Sliding)]
    public IActionResult ValidatePassword([FromForm] string userName, [FromForm] string password)
    {
        var res = userBl.ValidatePassword(userName, password);
        return res.Item1 ? Ok() : BadRequest(res.Item2);
    }


    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    public async Task<IActionResult> AccountSignIn([FromForm] RequestLoginModel request)
    {
        var authenticateState = userBl.Authenticate(request);
        if (authenticateState.Item1)
        {
            var claimsIdentity = userBl.CreateIdentity(request.UserName);
            var claimPrincipal = new ClaimsPrincipal(claimsIdentity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(ProtectorTime.CookieMaxAge)
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimPrincipal, authProperties);
            if (string.IsNullOrEmpty(request.ReturnUrl)) return Redirect("/");

            return Redirect(request.ReturnUrl);
        }

        if (!string.IsNullOrEmpty(request.ReturnUrl))
            return Redirect(PageRoutes.Account.SignInError.Src.AppendAndEncodeBase64StringAsUri([authenticateState.Item2, request.ReturnUrl]));
        return Redirect(PageRoutes.Account.SignInError.Src.AppendAndEncodeBase64StringAsUri(authenticateState.Item2));
    }


    [HttpPost("register")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    public async Task<IActionResult> AccountSignUp([FromForm] RequestRegisterModel request)
    {
        var user = userBl.Get(request.Username);
        if (user != null) return Redirect(PageRoutes.Account.SignInError.Src.AppendAndEncodeBase64StringAsUri(AppLang.User_is_already_exists));

        var validateContext = new ValidationContext(request, null, null);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(request, validateContext, validationResults, true);

        if (isValid)
        {
            var createResult = await userBl.CreateAsync(new UserModel
            {
                UserName = request.Username.ComputeSha256Hash(),
                Password = request.Password.ComputeSha256Hash(),
                BirthDay = request.BirthDay,
                FullName = request.FullName
            });

            if (createResult.IsSuccess) return Redirect("/api/Account/login");
            return Redirect(PageRoutes.Account.SignInError.Src.AppendAndEncodeBase64StringAsUri(createResult.Message));
        }

        foreach (var result in validationResults)
            if (result.ErrorMessage != null)
                return Redirect(PageRoutes.Account.SignInError.Src.AppendAndEncodeBase64StringAsUri(result.ErrorMessage));

        return Redirect(PageRoutes.Account.SignInError.Src.AppendAndEncodeBase64StringAsUri(AppLang.Registration_failed));
    }

    [HttpGet("sign-out")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AccountSignOut()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect(PageRoutes.Account.SignIn.Src);
    }

    [HttpPost("getJwt")]
    [EnableRateLimiting(PolicyNamesAndRoles.LimitRate.Fixed)]
    [IgnoreAntiforgeryToken]
    public IActionResult GetJwt([FromForm] RequestLoginModel request)
    {
        var authenticateState = userBl.Authenticate(request);
        if (authenticateState.Item1)
        {
            var claims = userBl.GetAllClaim(request.UserName);
            var token = jsonWebTokenCertificateProvider.GenerateJwtToken(claims);
            object model = new
            {
                Token = token,
                Message = AppLang.Do_not_use_this_token_on_a_server_that_provides_this_token
            };
            return Content(model.ToJson(), MediaTypeNames.Application.Json);
        }

        return Ok();
    }


    /// <summary>
    ///     Manual get AntiForgeryToken for your self. it's also add a cookie for you
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
            if (tokens.RequestToken != null)
                HttpContext.Response.Cookies.Append(cookieName!, tokens.RequestToken);

        return new JsonResult(new
        {
            token = tokens.RequestToken
        });
    }

    [HttpGet("get-never-expire-token")]
    [Authorize(Roles = "System")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetNeverExpireToken()
    {
        var userClaim = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
        if (userClaim == null)
        {
            ModelState.AddModelError("Permission", "Could not found user in the context");
            return BadRequest(ModelState);
        }

        var user = userBl.Get(userClaim.Value);
        if (user == null)
        {
            ModelState.AddModelError("Permission", "User can not be found");
            return BadRequest(ModelState);
        }

        var claims = userBl.GetAllClaim(userClaim.Value);

        var token = jsonWebTokenCertificateProvider.GenNeverExpireToken(userClaim.Value, claims);
        return Content(token.ToJson(), MediaTypeNames.Application.Json);
    }
}