using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Layout;

public partial class AppBar : ComponentBase
{
    public static object BreadCrumb = new();
    public static object TopBarSection1 = new();
    public static object TopBarSection2 = new();
    public static object TopBarSection3 = new();
    [Parameter] public EventCallback HandleDrawerOpen { get; set; }

    

   


  


    

  


    private async Task LogOut()
    {
        await JsRuntime.ClearLocalStorage();
        await JsRuntime.ClearSessionStorage();
        await JsRuntime.LocationReplace("api/Account/sign-out");
    }
}