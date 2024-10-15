using BusinessModels.System;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Business.Exceptions;

public class ErrorHandling(ILogger<ErrorHandling> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var recordModel = new ErrorRecordModel
        {
            Message = exception.Message,
            RequestId = httpContext.TraceIdentifier,
            Src = exception.Source ?? string.Empty,
            Href = httpContext.Request.Path
        };
        logger.LogError(exception, exception.Message);
        return ValueTask.FromResult(true);
    }
}