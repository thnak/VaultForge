using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Layout;

public partial class MainLayout
{
    private bool _drawerOpen;
    internal static object NavMenu { get; } = new();
    [CascadingParameter] private MudThemeProvider MudThemeProvider { get; set; } = default!;

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}