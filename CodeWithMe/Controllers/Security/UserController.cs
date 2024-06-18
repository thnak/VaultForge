using System.Security.Claims;
using BusinessModels.Secure;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Protector;

namespace CodeWithMe.Controllers.Security;

[ApiController]
[Route("api/[controller]/[action]")]
[ValidateAntiForgeryToken]
public class UserController : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] RequestLoginModel requestLoginModel)
    {
        if (requestLoginModel.Password == "haha" && requestLoginModel.UserName == "haha")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, requestLoginModel.UserName),
                new(ClaimTypes.NameIdentifier, requestLoginModel.UserName)
            };
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,// This ensures the cookie persists beyond the session
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieNames.AuthenticationType));
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);
            return Redirect($"/");
        }
        return Redirect($"/{requestLoginModel.ReturnUrl}");
    }
}