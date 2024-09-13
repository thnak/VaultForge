using Microsoft.AspNetCore.Http;

namespace Business.Utils.HttpExtension;

public static class HttpExtension
{
    /// <summary>
    /// Get cancel token
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static CancellationToken GetCancellationToken(this HttpContext context)
    {
        // 1. Define a 30-second grace period if a connection issue is detected
        var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var cancellationToken = context.RequestAborted;
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token).Token;
        return combinedToken;
    }
}