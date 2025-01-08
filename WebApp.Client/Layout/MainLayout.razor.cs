using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using WebApp.Client.Services.UserInterfaces;
using Timer = System.Timers.Timer;

namespace WebApp.Client.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private bool _drawerOpen = true;
    public static object NavMenu { get; } = new();
    [CascadingParameter] private MudThemeProvider MudThemeProvider { get; set; } = default!;
    private readonly Dictionary<string, object?> _drawerAttribute = new() { { "id", Guid.NewGuid().ToString() } };
    private Timer? _timer;
    private Dictionary<string, object?> _appbarAttribute = new() { { "id", Guid.NewGuid().ToString() } };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            string[] elementIds = [_drawerAttribute["id"]!.ToString()!, _appbarAttribute["id"]!.ToString()!];
            foreach (string elementId in elementIds)
            {
                await EventListener.AddEventListenerAsync(elementId, DomEventName.MouseOver, DrawerHover);
                await EventListener.AddEventListenerAsync(elementId, DomEventName.MouseOut, DrawerMouseOut);
            }

            Navigation.LocationChanged += NavigationManagerOnLocationChanged;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void InitTimer()
    {
        _timer = new Timer(5000) { AutoReset = true };
        _timer.Elapsed += AutoCloseMenuTimerOnElapsed;
        _timer.Start();
    }

    private void AutoCloseMenuTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        _drawerOpen = false;
        InvokeAsync(StateHasChanged);
    }

    private Task DrawerMouseOut()
    {
        if (_timer != null) _timer.Start();
        return Task.CompletedTask;
    }

    private Task DrawerHover()
    {
        if (_timer != null) _timer.Stop();
        return Task.CompletedTask;
    }

    private void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (_timer == null)
            InitTimer();
    }


    private Task DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
        return InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        _timer?.Dispose();
        _timer = null;
        Navigation.LocationChanged -= NavigationManagerOnLocationChanged;
        string[] elementIds = [_drawerAttribute["id"]!.ToString()!, _appbarAttribute["id"]!.ToString()!];
        foreach (string elementId in elementIds)
        {
            await EventListener.RemoveEventListenerAsync(elementId, DomEventName.MouseOver);
            await EventListener.RemoveEventListenerAsync(elementId, DomEventName.MouseOut);
        }
    }
}