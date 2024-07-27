using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Layout;

public partial class AppBar : ComponentBase, IDisposable
{
    public static object TopBarSection1 = new();
    public static object TopBarSection2 = new();

    [Parameter] public EventCallback HandleDrawerOpen { get; set; }
    [Parameter] public MudThemeProvider? MudThemeProvider { get; set; } = default!;

    private bool? IsDarkMode { get; set; }

    public void Dispose()
    {
        CustomStateContainer.OnChanged -= StateHasChanged;
    }


    protected override Task OnInitializedAsync()
    {
        CustomStateContainer.OnChanged += StateHasChanged;
        return Task.CompletedTask;
    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var isDarkMode = await JsRuntime.InvokeAsync<string?>("localStorage.getItem", nameof(CustomStateContainer.IsDarkMode));
            if (isDarkMode != null)
            {
                IsDarkMode = bool.Parse(isDarkMode);
                CustomStateContainer.IsDarkMode = bool.Parse(isDarkMode);
            }
            else
            {
                IsDarkMode = null;
            }
        }
    }

    private async Task ThemeModeChanged()
    {
        switch (IsDarkMode)
        {
            case null:
                IsDarkMode = true;
                CustomStateContainer.IsDarkMode = IsDarkMode.Value;
                await JsRuntime.SetLocalStorage(nameof(CustomStateContainer.IsDarkMode), IsDarkMode.Value);
                break;
            case true:
                IsDarkMode = false;
                CustomStateContainer.IsDarkMode = IsDarkMode.Value;
                await JsRuntime.SetLocalStorage(nameof(CustomStateContainer.IsDarkMode), IsDarkMode.Value);
                break;
            case false:
                IsDarkMode = null;
                if (MudThemeProvider != null) CustomStateContainer.IsDarkMode = await MudThemeProvider.GetSystemPreference();
                await JsRuntime.RemoveLocalStorage(nameof(CustomStateContainer.IsDarkMode));
                break;
        }


        if (MudThemeProvider != null) CustomStateContainer.IsDarkMode = MudThemeProvider.IsDarkMode;

        await InvokeAsync(StateHasChanged);
    }


    private async Task LogOut()
    {
        await JsRuntime.ClearLocalStorage();
        await JsRuntime.ClearSessionStorage();
        await JsRuntime.LocationReplace("api/Account/sign-out");
    }
}