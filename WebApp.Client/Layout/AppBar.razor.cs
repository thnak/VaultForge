using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Layout;

public partial class AppBar : ComponentBase, IDisposable
{
    internal static object TopBarSection1 = new();
    internal static object TopBarSection2 = new();
    internal static object TopBarSection3 = new();
    internal static object TopBarSectionN = new();
    [Parameter] public EventCallback HandleDrawerOpen { get; set; }
    private Dictionary<string, object?> MenuBarButtonAttribute => new() { { "aria-label", "nav menu button" } };

    private Color OnLineColor { get; set; } = Color.Success;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            EventListener.Online += Online;
            EventListener.Offline += Offline;
        }

        base.OnAfterRender(firstRender);
    }

    private void Offline()
    {
        OnLineColor = Color.Dark;
        InvokeAsync(StateHasChanged);
    }

    private void Online()
    {
        OnLineColor = Color.Success;
        InvokeAsync(StateHasChanged);
    }

    private async Task LogOut()
    {
        await JsRuntime.ClearLocalStorage();
        await JsRuntime.ClearSessionStorage();
        await JsRuntime.LocationReplace("api/Account/sign-out");
    }

    public void Dispose()
    {
        EventListener.Online -= Online;
        EventListener.Offline -= Offline;
    }
}