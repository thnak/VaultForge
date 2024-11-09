using Business.SignalRHub.System.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Business.SignalRHub.System.Implement;

public class ClockHub : Hub<IClock>
{
    public async Task SendTimeToClients(DateTime dateTime)
    {
        await Clients.All.ShowTime(dateTime);
    }
}