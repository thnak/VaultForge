using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Web.Client.Services.Http;

namespace Web.Client.Pages;

public partial class Home(BaseHttpClientService apiService) : ComponentBase, IDisposable
{
    public void Dispose()
    {
        ProtectedLocalStorageService.KeyHandler -= GetKey;
    }

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

    private async Task GetWeather(MouseEventArgs obj)
    {
        var response = await apiService.HttpClient.GetAsync("/WeatherForecast/GetWeatherForecast");
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStringAsync();
            Console.WriteLine(data.Length);
        }
    }
}