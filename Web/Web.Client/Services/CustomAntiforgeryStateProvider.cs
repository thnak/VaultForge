using BusinessModels.Resources;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Web.Client.Utils;

namespace Web.Client.Services;

public class CustomAntiforgeryStateProvider(AntiforgeryStateProvider antiforgeryStateProvider, IJSRuntime jsRuntime)
{
    public AntiforgeryRequestToken? GetAntiforgeryToken()
    {
        var token = antiforgeryStateProvider.GetAntiforgeryToken();
        if (token == null)
        {
            var cookie = jsRuntime.GetCookie(CookieNames.Antiforgery).Result;
            if (cookie != null)
            {
                token = new AntiforgeryRequestToken(cookie, CookieNames.Antiforgery);
            }
        }

        return token;
    }

    public async Task<AntiforgeryRequestToken?> GetAntiforgeryTokenAsync()
    {
        var token = antiforgeryStateProvider.GetAntiforgeryToken();
        if (token == null)
        {
            var cookie = await jsRuntime.GetCookie(CookieNames.Antiforgery);
            if (cookie != null)
            {
                token = new AntiforgeryRequestToken(cookie, CookieNames.Antiforgery);
            }
        }

        return token;
    }
}