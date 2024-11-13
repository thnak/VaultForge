namespace WebApp.Utils;

public static class BlazorSsrRedirectManagerExtensions
{
    public static void RedirectTo(this HttpContext httpContext, string redirectionUrl)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.Headers.Append("blazor-enhanced-nav-redirect-location", redirectionUrl);
        httpContext.Response.StatusCode = 200;
        httpContext.Response.Redirect(redirectionUrl);
    }
}