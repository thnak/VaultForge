using MudBlazor;
using WebApp.Client.Assets;

namespace WebApp.Client.Services.UserInterfaces;

public class StateContainer
{
    private bool _isDarkMode;

    private MudTheme _mudTheme;

    public StateContainer()
    {
        _mudTheme = StaticThemes.DefaultTheme;
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


    private void HandleChanged()
    {
        SharedPalette = _isDarkMode ? _mudTheme.PaletteDark : _mudTheme.PaletteLight;
        OnChanged?.Invoke();
        OnChangedAsync?.Invoke();
    }
}