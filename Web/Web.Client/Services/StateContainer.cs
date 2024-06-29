using Microsoft.JSInterop;
using MudBlazor;

namespace Web.Client.Services;

public class StateContainer
{
    private bool _isDarkMode;

    private MudTheme _mudTheme;

    public StateContainer()
    {
        _mudTheme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#00a3e8",
                PrimaryContrastText = "#F8F8F8",
                AppbarBackground = "#ffffff",
                DrawerIcon = "#68b7d9",
                AppbarText = "#000000",
                Warning = Colors.Amber.Default
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#00a3e8",
                DrawerIcon = "#68b7d9"
            }
        };
        SharedPalette = _isDarkMode ? _mudTheme.PaletteDark : _mudTheme.PaletteLight;
    }

    public Action? OnChanged { get; set; }
    public Func<Task>? OnChangedAsync { get; set; }
    public Palette SharedPalette { get; set; }

    public MudTheme MudTheme
    {
        get => _mudTheme;
        set
        {
            _mudTheme = value;
            HandleChanged();
        }
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _isDarkMode = value;
            HandleChanged();
        }
    }
    
    [JSInvokable]
    public static void ReceiveScreenshot(string dataUrl)
    {
        Console.WriteLine(dataUrl);
    }
    
    private void HandleChanged()
    {
        SharedPalette = _isDarkMode ? _mudTheme.PaletteDark : _mudTheme.PaletteLight;
        OnChanged?.Invoke();
        OnChangedAsync?.Invoke();
    }
}