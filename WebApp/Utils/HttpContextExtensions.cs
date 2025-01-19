using System.Text;
using System.Web;

namespace WebApp.Utils;

public static class HttpContextExtensions
{
    public static string BuildNewUriApi(this HttpContext context, Dictionary<string, string?> parameters)
    {
        StringBuilder uriBuilder = new();
        uriBuilder.Append(context.Request.Scheme);
        uriBuilder.Append("://");
        uriBuilder.Append(context.Request.Host.Value);
        uriBuilder.Append(context.Request.Path);
        if (parameters.Count > 0)
        {
            uriBuilder.Append("?");
            foreach (var pair in parameters) uriBuilder.Append($"{pair.Key}={HttpUtility.UrlEncode(pair.Value)}&");
        }

        return uriBuilder.ToString();
    }
}