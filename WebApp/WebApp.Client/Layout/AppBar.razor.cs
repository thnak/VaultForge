using Microsoft.AspNetCore.Components;
using WebApp.Client.Utils;

namespace WebApp.Client.Layout;

public partial class AppBar : ComponentBase
{
    internal static object TopBarSection1 = new();
    internal static object TopBarSection2 = new();
    internal static object TopBarSection3 = new();
    internal static object TopBarSectionN = new();
    [Parameter] public EventCallback HandleDrawerOpen { get; set; }
    private Dictionary<string, object?> MenuBarButtonAttribute => new() { { "aria-label", "nav menu button" } };


    private async Task LogOut()
    {
        await JsRuntime.ClearLocalStorage();
        await JsRuntime.ClearSessionStorage();
        await JsRuntime.LocationReplace("api/Account/sign-out");
    }
}