using MudBlazor;

namespace WebApp.Client.Services.UserInterfaces;

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
                Primary = "#3A6D8C",
                Secondary = "#6A9AB0",
                Tertiary = "#EAD8B1",
                PrimaryContrastText = "#F8F8F8",
                AppbarBackground = "#001F3F",
                Background = "#001F3F",
                Surface = "#001F3F",
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


    private void HandleChanged()
    {
        SharedPalette = _isDarkMode ? _mudTheme.PaletteDark : _mudTheme.PaletteLight;
        OnChanged?.Invoke();
        OnChangedAsync?.Invoke();
    }
}