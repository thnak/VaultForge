namespace Web.MiddleWares;

public class ErrorHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.Redirect($"/Error/{Uri.EscapeDataString(exception.Message)}");
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }
}