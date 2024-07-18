using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Business.Exceptions;

public class ErrorHandling : IExceptionHandler
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

        httpContext.Response.Redirect($"{PageRoutes.Error.ErrorPage.AppendAndEncodeBase64StringAsUri(recordModel.Encode2Base64String())}");
        return ValueTask.FromResult(true);
    }
}