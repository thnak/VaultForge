using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.Utils;

namespace ResApi.Middleware;

public class GlobalMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // context.Response.OnStarting(() =>
            // {
            //     context.Response.Headers.Append("Content-Security-Policy", "default-src 'self';");
            //     context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            //     context.Response.Headers.Append("X-Frame-Options", "DENY");
            //     context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
            //     context.Response.Headers.Append("Referrer-Policy", "no-referrer");
            //
            //     return Task.CompletedTask;
            // });
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var recordModel = new ErrorRecordModel
        {
            Message = exception.Message,
            RequestId = context.TraceIdentifier,
            Src = exception.Source ?? string.Empty,
            Href = context.Request.Path
        };

        context.Response.Redirect($"{PageRoutes.Error.ErrorPage.AppendAndEncodeBase64StringAsUri(recordModel.Encode2Base64String())}");
        return Task.CompletedTask;
    }
}