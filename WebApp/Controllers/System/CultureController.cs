﻿using BusinessModels.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.System;

[Route("[controller]/[action]")]
public class CultureController : Controller
{
    // Set Culture
    public IActionResult Set(string? culture, string? redirectUri)
    {
        if (culture is not null)
        {
            var cookieTextPlant = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture, culture));
            HttpContext.Response.Cookies.Append(
                CookieNames.Culture,
                cookieTextPlant,
                new CookieOptions
                {
                    IsEssential = true,
                    Expires = DateTimeOffset.MaxValue
                }
            );
        }

        if (!string.IsNullOrEmpty(redirectUri)) return LocalRedirect(redirectUri);
        return Ok();
    }
}