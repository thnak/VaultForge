using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages;

public partial class Home(ILogger<Home> logger) : ComponentBase, IDisposable
{
    public void Dispose()
    {
        ProtectedLocalStorageService.KeyHandler -= GetKey;
        CustomStateContainer.OnChangedAsync -= UpdateChart;
        ChartRef?.Dispose();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            ProtectedLocalStorageService.KeyHandler += GetKey;
            CustomStateContainer.OnChangedAsync += UpdateChart;
        }

        base.OnAfterRender(firstRender);
    }

    protected override async Task OnInitializedAsync()
    {
        await UpdateChart();
        await base.OnInitializedAsync();
    }

    private async Task UpdateChart()
    {
        var mode = CustomStateContainer.IsDarkMode ? Mode.Dark : Mode.Light;
        ChartOption.Theme.Mode = mode;
        await ChartRef.UpdateOptionsSafetyAsync();
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
        logger.LogInformation(data);
    }

    private async Task GetWeather(MouseEventArgs obj)
    {
        var response = await ApiService.HttpClient!.GetAsync("/WeatherForecast/GetWeatherForecast");
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadAsStringAsync();
            logger.LogInformation(data.Length.ToString());
        }
    }

    private ApexChart<MyData>? ChartRef { get; set; }
    public ApexChartOptions<MyData> ChartOption { get; set; } = ApexChartExtension.InitChartOptions<MyData>();
}