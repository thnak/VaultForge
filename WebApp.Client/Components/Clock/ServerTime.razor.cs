using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebApp.Client.Components.Clock;

public partial class ServerTime(ILogger<ServerTime> logger) : ComponentBase, IAsyncDisposable
{
    private HubConnection? hubConnection { get; set; }
    private DateTime Date { get; set; }
    private string ElementId { get; set; } = Guid.NewGuid().ToString();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.BaseUri + "hubs/clock")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect()
                .Build();
            hubConnection.On<DateTime>("ShowTime", ShowTime);
            await hubConnection.StartAsync();
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
        if (hubConnection != null)
        {
            logger.LogInformation($"Disposing ServerTime {ElementId}");
            await hubConnection.DisposeAsync();
        }
    }
}