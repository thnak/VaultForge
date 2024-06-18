using System.Security.Cryptography;
using BusinessModels.Resources;

namespace CodeWithMe.MiddleWares;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Cookie decryption failed, clearing cookie and redirecting to login.");
            // Clear the corrupted cookie
            context.Response.Cookies.Delete(CookieNames.AuthorizeCookie);
            // Redirect to login page
            context.Response.Redirect("/Account/Login?ReturnUrl=" + context.Request.Path);
        }
        catch (Exception ex)
        {
            // Log other exceptions
            logger.LogError(ex, "An unexpected error occurred.");
            throw;
        }
    }
}