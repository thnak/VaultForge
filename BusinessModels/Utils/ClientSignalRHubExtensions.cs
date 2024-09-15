using BusinessModels.Converter;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessModels.Utils;

public static class ClientSignalRHubExtensions
{
    public static HubConnection InitHub(this Uri uri, bool useMessagePack = true)
    {
        var hubConnectionBuilder = new HubConnectionBuilder()
            .WithUrl(uri)
            .AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new ObjectIdConverter()); });

        if (useMessagePack)
        {
            hubConnectionBuilder.AddMessagePackProtocol(options =>
            {
                options.SerializerOptions = MessagePackSerializerOptions.Standard
                    .WithResolver(StaticCompositeResolver.Instance)
                    .WithSecurity(MessagePackSecurity.UntrustedData);
            });
        }

        return hubConnectionBuilder.Build();
    }
}