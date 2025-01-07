using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Services.UserInterfaces;

namespace WebApp.Client.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private bool _drawerOpen = true;
    public static object NavMenu { get; } = new();
    [CascadingParameter] private MudThemeProvider MudThemeProvider { get; set; } = default!;
    private readonly Dictionary<string, object?> _drawerAttribute = new() { { "id", Guid.NewGuid().ToString() } };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await EventListener.AddEventListenerAsync(_drawerAttribute["id"]!.ToString()!, DomEventName.Click, DrawerHover);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task DrawerHover()
    {
        return Task.CompletedTask;
    }

    private Task DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
        return InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await EventListener.RemoveEventListenerAsync(_drawerAttribute["id"]!.ToString()!, DomEventName.Click);
    }
}