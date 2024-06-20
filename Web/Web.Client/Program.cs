using Blazored.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Web.Client.Authenticate;
using Web.Client.Services;

namespace Web.Client;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddMudServices();
        builder.Services.AddBlazoredToast();
        builder.Services.AddSingleton<StateContainer>();

        
        builder.Services.AddAuthorizationCore();
        builder.Services.AddCascadingAuthenticationState();
        // builder.Services.AddAuthenticationStateDeserialization();
        builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();

        await builder.Build().RunAsync();
    }
}