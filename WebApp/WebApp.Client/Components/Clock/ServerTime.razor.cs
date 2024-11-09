using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebApp.Client.Components.Clock;

public partial class ServerTime : ComponentBase, IAsyncDisposable
{
    private HubConnection? hubConnection { get; set; }
    private DateTime Date { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7158/hubs/clock")
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
        if (hubConnection != null) await hubConnection.DisposeAsync();
    }
}