using Business.SignalRHub.System.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Business.SignalRHub.System.Implement;

public class IoTSensorSignalHub : Hub<IIoTSensorSignal>
{
    public async Task JoinSensorGroup(string sensorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sensorId);
        await Clients.Groups(sensorId).ReceiveMessage($"Joined group {sensorId}");
    }

    public async Task LeaveSensorGroup(string sensorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sensorId);
        await Clients.Groups(sensorId).ReceiveMessage($"Left group {sensorId}");
    }

    /// <summary>
    /// Broadcast data to all clients in a specific sensor group
    /// </summary>
    /// <param name="sensorId"></param>
    /// <param name="message"></param>
    public async Task SendMessageToGroup(string sensorId, string message)
    {
        await Clients.Group(sensorId).ReceiveMessage(message);
    }

    public async Task SendCountToGroup(string sensorId, ulong count)
    {
        await Clients.Group(sensorId).ReceiveCount(count);
    }

    public async Task SendValueToGroup(string sensorId, double value)
    {
        await Clients.Group(sensorId).ReceiveValue(value);
    }
}