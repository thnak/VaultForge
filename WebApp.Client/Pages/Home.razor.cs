using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace WebApp.Client.Pages;

public partial class Home : ComponentBase, IDisposable
{
    public void Dispose()
    {
        ProtectedLocalStorageService.KeyHandler -= GetKey;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            ProtectedLocalStorageService.KeyHandler += GetKey;
        }

        base.OnAfterRender(firstRender);
    }

    private async Task Crypting(MouseEventArgs obj)
    {
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

    private async Task GetWeather(MouseEventArgs obj)
    {
        var response = await ApiService.HttpClient!.GetAsync("/WeatherForecast/GetWeatherForecast");
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStringAsync();
            Console.WriteLine(data.Length);
        }
    }
}