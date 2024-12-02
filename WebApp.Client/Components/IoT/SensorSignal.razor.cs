using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace WebApp.Client.Components.IoT;

public partial class SensorSignal(ILogger<SensorSignal> logger) : ComponentBase, IAsyncDisposable
{
    [Parameter] public string SensorId { get; set; } = string.Empty;
    private HubConnection? HubConnection { get; set; }
    private ulong CountValue { get; set; }
    private string ElementId { get; set; } = Guid.NewGuid().ToString();
    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var cancellationToken = CancellationTokenSource.Token;
            var value = await ApiService.GetAsync<ulong>($"/api/v1/get-count/{SensorId}", cancellationToken);
            if (value.IsSuccessStatusCode)
                CountValue = value.Data;
            HubConnection = new HubConnectionBuilder().InitConnection(Navigation.BaseUri + "hubs/iotSensor");
            HubConnection.On<ulong>("ReceiveCount", ShowValue);
            HubConnection.On<string>("ReceiveMessage", ReceiveMessage);
            await HubConnection.StartAsync(cancellationToken);
            await HubConnection.InvokeAsync("JoinSensorGroup", SensorId, cancellationToken: cancellationToken);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task ReceiveMessage(string arg)
    {
        logger.LogInformation(arg);
        return Task.CompletedTask;
    }

    public Task ShowValue(ulong currentTime)
    {
        CountValue = currentTime;
        return InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (HubConnection != null)
        {
            await HubConnection.InvokeAsync("LeaveSensorGroup", SensorId);
            logger.LogInformation($"Disposing ServerTime {ElementId}");
            await HubConnection.DisposeAsync();
        }


        await CancellationTokenSource.CancelAsync();
        CancellationTokenSource.Dispose();
    }
}