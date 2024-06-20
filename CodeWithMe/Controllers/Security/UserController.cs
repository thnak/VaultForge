using System.Security.Claims;
using BusinessModels.Resources;
using BusinessModels.Secure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Protector.Certificates;

namespace CodeWithMe.Controllers.Security;

[ApiController]
[Route("api/[controller]/[action]")]
[ValidateAntiForgeryToken]
public class UserController(JsonWebTokenCertificateProvider jsonWebTokenCertificateProvider) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] RequestLoginModel requestLoginModel)
    {
        if (requestLoginModel is { Password: "haha", UserName: "haha" })
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, requestLoginModel.UserName),
                new(ClaimTypes.NameIdentifier, requestLoginModel.UserName),

            };
            claims.Add(new Claim(ClaimTypes.UserData, jsonWebTokenCertificateProvider.GenerateJwtToken(claims)));
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false// This ensures the cookie persists beyond the session
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieNames.AuthenticationType));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);
            // return Redirect($"/{requestLoginModel.ReturnUrl}");
        }
        return Redirect($"/{requestLoginModel.ReturnUrl}");
    }
}