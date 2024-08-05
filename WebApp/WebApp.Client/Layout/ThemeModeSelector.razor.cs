using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Layout;

public partial class ThemeModeSelector : ComponentBase, IDisposable
{
    [Parameter] public MudThemeProvider? MudThemeProvider { get; set; } = default!;
    private bool? IsDarkMode { get; set; }
    private MudTheme? Theme { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            CustomStateContainer.OnChanged += StateHasChanged;
            Theme = CustomStateContainer.MudTheme;
            if (MudThemeProvider != null) await MudThemeProvider.WatchSystemPreference(WatchSystemPreference);
            var isDarkMode = await JsRuntime.GetLocalStorage(nameof(CustomStateContainer.IsDarkMode));
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

    private Task WatchSystemPreference(bool mode)
    {
        if (IsDarkMode == null)
        {
            if (MudThemeProvider != null)
            {
                CustomStateContainer.IsDarkMode = mode;
                InvokeAsync(StateHasChanged);
            }
        }

        return Task.CompletedTask;
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

    public void Dispose()
    {
        CustomStateContainer.OnChanged -= StateHasChanged;
    }
}