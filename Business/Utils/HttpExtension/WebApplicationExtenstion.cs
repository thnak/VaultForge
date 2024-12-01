using Business.SignalRHub.System.Implement;
using Business.SocketHubs;
using BusinessModels.Converter;
using BusinessModels.Utils;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Utils.HttpExtension;

public static class WebApplicationExtenstion
{
    public static void MapSignalRHubs(this WebApplication app)
    {
        app.MapHub<PageCreatorHub>("/PageCreatorHub");
        app.MapHub<ClockHub>("/hubs/clock");
    }

    public static void AddSignalRService(this IServiceCollection services)
    {
        StaticCompositeResolver.Instance.Register(
            StandardResolver.Instance,
            NativeDecimalResolver.Instance,
            NativeGuidResolver.Instance,
            NativeDateTimeResolver.Instance,
            MongoObjectIdResolver.Instance);

        services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = int.MaxValue;
                options.MaximumParallelInvocationsPerClient = 100;
            }).AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new ObjectIdConverter()); })
            .AddMessagePackProtocol(options => { options.SerializerOptions = ClientSignalRHubExtensions.GetMessagePackSerializerOptions(); });
    }
}