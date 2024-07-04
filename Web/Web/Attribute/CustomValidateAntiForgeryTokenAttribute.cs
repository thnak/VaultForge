using BusinessModels.Resources;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Web.Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CustomValidateAntiForgeryTokenAttribute() : TypeFilterAttribute(typeof(CustomValidateAntiForgeryTokenFilter))
{
    private class CustomValidateAntiForgeryTokenFilter(IAntiforgery antiforgery, IHttpContextAccessor httpContextAccessor) : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;
            var tokens = antiforgery.GetAndStoreTokens(context.HttpContext);

            var headerToken = request.Headers[CookieNames.Antiforgery].ToString();
            var formToken = request.Form["__RequestVerificationToken"].ToString();

            var cookieToken = tokens.RequestToken;

            if (!string.IsNullOrEmpty(headerToken))
            {
                // Validate the token from the header
                if (!antiforgery.IsRequestValidAsync(context.HttpContext).GetAwaiter().GetResult())
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            else if (!string.IsNullOrEmpty(formToken))
            {
                // Validate the token from the form
                if (!antiforgery.IsRequestValidAsync(context.HttpContext).GetAwaiter().GetResult())
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            else
            {
                // Fallback to validating the token from the cookie
                if (string.IsNullOrEmpty(cookieToken) || !antiforgery.IsRequestValidAsync(context.HttpContext).GetAwaiter().GetResult())
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }
    }
}