using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Net.Http.Headers;

namespace WebApp.Components;

public partial class App(ILogger<App> logger)
{
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;
    private bool IsBot { get; set; }
    private bool IsWasm { get; set; }

    protected override void OnInitialized()
    {
        var context = HttpContextAccessor?.HttpContext;
        var userAgent = context?.Request.Headers[HeaderNames.UserAgent];
        if (userAgent.HasValue)
        {
            var agent = userAgent.ToString();
            if (!string.IsNullOrWhiteSpace(agent))
            {
                IsBot = Regex.IsMatch(agent, @"bot|crawler|baiduspider|80legs|ia_archiver|voyager|curl|wget|yahoo! slurp|mediapartners-google", RegexOptions.IgnoreCase);
                if (IsBot)
                    logger.LogInformation("[BOT][True]");
            }
        }

        logger.LogInformation($"[IP|{context?.Connection.RemoteIpAddress?.ToString()}]");
        base.OnInitialized();
    }

    private IComponentRenderMode RenderModeForPage()
    {
        IsWasm = false;
        var mode = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RenderModeAttribute>()?.Mode;
        if (mode == null) return new InteractiveServerRenderMode();
        if (mode is InteractiveWebAssemblyRenderMode)
        {
            IsWasm = true;
            return new InteractiveWebAssemblyRenderMode(false);
        }

        return mode;
    }
}