﻿using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using WebApp.Client.Utils;

namespace WebApp.Client.Components.IoT;

public partial class SensorSignal(ILogger<SensorSignal> logger) : ComponentBase, IAsyncDisposable
{
    [Parameter] public string SensorId { get; set; } = string.Empty;
    private HubConnection? HubConnection { get; set; }
    private ulong CountValue { get; set; }
    private float Value { get; set; }
    private string ElementId { get; set; } = Guid.NewGuid().ToString();
    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var cancellationToken = CancellationTokenSource.Token;
            var value = await ApiService.GetAsync<ulong>($"/api/iot/v1/get-count/{SensorId}", cancellationToken);
            if (value.IsSuccessStatusCode)
            {
                CountValue = value.Data;
                await InvokeAsync(StateHasChanged);
            }

            HubConnection = new HubConnectionBuilder().InitConnection(Navigation.BaseUri + "hubs/iotSensor");
            HubConnection.On<ulong>("ReceiveCount", ShowValue);
            HubConnection.On<float>("ReceiveValue", ReceiveValue);
            HubConnection.On<string>("ReceiveMessage", ReceiveMessage);
            HubConnection.Reconnected += HubConnectionOnReconnected;
            HubConnection.Reconnecting += HubConnectionOnReconnecting;
            await HubConnection.StartAsync(cancellationToken);
            EventListener.PageHideEventAsync += PageHideEvent;
            EventListener.PageShowEventAsync += PageShowEvent;
            await HubConnection.InvokeAsync("JoinSensorGroup", SensorId, cancellationToken: cancellationToken);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task HubConnectionOnReconnecting(Exception? arg)
    {
        if (arg is not null)
        {
            ToastService.ShowError(arg.Message, TypeClassList.ToastDefaultSetting);
            return Task.CompletedTask;
        }

        ToastService.ShowSuccess("Connecting..", TypeClassList.ToastDefaultSetting);
        return Task.CompletedTask;
    }

    private Task HubConnectionOnReconnected(string? arg)
    {
        ToastService.ShowSuccess($"Reconnected to Hub: {arg}", TypeClassList.ToastDefaultSetting);
        if (HubConnection != null) return HubConnection.InvokeAsync("JoinSensorGroup", SensorId);
        return Task.CompletedTask;
    }

    private Task ReceiveValue(float arg)
    {
        Value = arg;
        return InvokeAsync(StateHasChanged);
    }

    private async Task PageShowEvent()
    {
        if (HubConnection != null)
        {
            await HubConnection.StartAsync();
        }
    }

    private async Task PageHideEvent()
    {
        if (HubConnection != null)
        {
            await HubConnection.StopAsync();
        }
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
            HubConnection.Reconnected -= HubConnectionOnReconnected;
            await HubConnection.InvokeAsync("LeaveSensorGroup", SensorId);
            logger.LogInformation($"Disposing ServerTime {ElementId}");
            await HubConnection.DisposeAsync();
        }

        EventListener.PageHideEventAsync -= PageHideEvent;
        EventListener.PageShowEventAsync -= PageShowEvent;

        await CancellationTokenSource.CancelAsync();
        CancellationTokenSource.Dispose();
    }
}