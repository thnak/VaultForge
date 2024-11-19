using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebApp.Client.Components.Clock;

public partial class ServerTime(ILogger<ServerTime> logger) : ComponentBase, IAsyncDisposable
{
    private HubConnection? HubConnection { get; set; }
    private DateTime Date { get; set; }
    private string ElementId { get; set; } = Guid.NewGuid().ToString();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.BaseUri + "hubs/clock")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect()
                .Build();
            HubConnection.On<DateTime>("ShowTime", ShowTime);
            await HubConnection.StartAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public Task ShowTime(DateTime currentTime)
    {
        Date = currentTime;
        return InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (HubConnection != null)
        {
            logger.LogInformation($"Disposing ServerTime {ElementId}");
            await HubConnection.DisposeAsync();
        }
    }
}