using Business.SignalRHub.System.Implement;
using Business.SocketHubs;

namespace WebApp.Utils;

public static class WebApplicationExtenstion
{
    public static void MapSignalRHubs(this WebApplication app)
    {
        app.MapHub<PageCreatorHub>("/PageCreatorHub");
        app.MapHub<ClockHub>("/hubs/clock");
    }
}