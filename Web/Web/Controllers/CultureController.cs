using BusinessModels.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Route("[controller]/[action]")]
public class CultureController : Controller
{
    // Set Culture
    public IActionResult Set(string? culture, string? redirectUri)
    {
        if (culture is not null)
            HttpContext.Response.Cookies.Append(
                CookieNames.Culture,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture)),
                new CookieOptions()
                {
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddYears(1)
                }
            );

        if (!string.IsNullOrEmpty(redirectUri)) return LocalRedirect(redirectUri);
        return Ok();
    }
}