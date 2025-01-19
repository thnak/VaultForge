using BusinessModels.Resources;
using Microsoft.AspNetCore.Localization;

#if DEBUG
#else
using BusinessModels.Utils;
#endif

namespace WebApp.MiddleWares;

public class Middleware(RequestDelegate next, ILogger<Middleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
                context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
                context.Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");

                if (context.Request.Query.TryGetValue("lang", out var lang))
                {
                    var langCode = lang.ToString();
                    if (AllowedCulture.SupportedCultures.Any(x => x.Name == langCode))
                        if (!string.IsNullOrWhiteSpace(langCode))
                            SetLanguageCookie(context, langCode);
                }
                else
                {
                    if (context.Request.Cookies.Count > 0 && !context.Request.Cookies.TryGetValue(CookieNames.Culture, out _))
                        if (context.Request.Headers.TryGetValue("Accept-Language", out var acceptLang))
                        {
                            var langCode = acceptLang.ToString();
                            if (AllowedCulture.SupportedCultures.Any(x => x.Name == langCode))
                            {
                                var preferredLanguage = langCode.Split(',').FirstOrDefault();
                                if (!string.IsNullOrWhiteSpace(preferredLanguage)) SetLanguageCookie(context, preferredLanguage);
                            }
                        }
                }

                return Task.CompletedTask;
            });
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        logger.LogError(exception, context.Request.Method + " " + context.Request.PathBase + context.Request.Path + context.Request.QueryString);
#if DEBUG
        throw exception;
#else
var recordModel = new ErrorRecordModel
        {
            Message = exception.Message,
            RequestId = context.TraceIdentifier,
            Src = exception.Source ?? string.Empty,
            Href = context.Request.Path
        };
        context.Response.Redirect($"{PageRoutes.Error.ErrorPage.AppendAndEncodeBase64StringAsUri(recordModel.Encode2Base64String())}");
        return Task.CompletedTask;
#endif
    }

    private void SetLanguageCookie(HttpContext context, string language)
    {
        var cookieTextPlant = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(language, language));
        context.Response.Cookies.Append(
            CookieNames.Culture,
            cookieTextPlant,
            new CookieOptions
            {
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            }
        );
    }
}