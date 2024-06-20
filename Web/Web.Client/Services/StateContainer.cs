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
            Palette = new PaletteLight
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
        SharedPalette = _isDarkMode ? _mudTheme.PaletteDark : _mudTheme.Palette;
    }

    public Action? OnChanged { get; set; }

#pragma warning disable CS0618
    public Palette SharedPalette { get; set; }
#pragma warning restore CS0618

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

    // public SidebarType SidebarType { get; set; }

    private void HandleChanged()
    {
        SharedPalette = _isDarkMode ? _mudTheme.PaletteDark : _mudTheme.Palette;
        OnChanged?.Invoke();
    }
}