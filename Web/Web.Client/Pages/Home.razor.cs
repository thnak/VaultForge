using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Web.Client.Pages;

public partial class Home : ComponentBase, IDisposable
{
    private async Task Crypting(MouseEventArgs obj)
    {
        ProtectedLocalStorageService.KeyHandler += GetKey;
        await ProtectedLocalStorageService.SetAsync("exampleKey", "This is a protected value");
    }

    private Task<string> GetKey()
    {
        return Task.FromResult("haha");
    }

    private async Task DeCrypting(MouseEventArgs obj)
    {
        var data = await ProtectedLocalStorageService.GetAsync("exampleKey");
        Console.WriteLine(data);
    }
    public void Dispose()
    {
        ProtectedLocalStorageService.KeyHandler -= GetKey;
    }
}