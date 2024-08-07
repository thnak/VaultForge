using Microsoft.AspNetCore.Components;
using WebApp.Client.Utils;

namespace WebApp.Client.Layout;

public partial class AppBar : ComponentBase
{
    internal static object BreadCrumb = new();
    internal static object TopBarSection1 = new();
    internal static object TopBarSection2 = new();
    internal static object TopBarSection3 = new();
    [Parameter] public EventCallback HandleDrawerOpen { get; set; }


    private async Task LogOut()
    {
        await JsRuntime.ClearLocalStorage();
        await JsRuntime.ClearSessionStorage();
        await JsRuntime.LocationReplace("api/Account/sign-out");
    }
}