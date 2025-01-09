using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

    private List<MyData> Data { get; set; } = new();

    protected override void OnInitialized()
    {
        DateTime from = DateTime.UtcNow.AddDays(-10);
        DateTime to = DateTime.Today;
        Data.Clear();
        Random random = new();
        for (DateTime i = from; i < to; i = i.AddDays(1))
        {
            Data.Add(new MyData { Category = i, NetProfit = random.Next(3000, 6000), StuPid = random.Next(3000, 6000) });
        }
    }

    public class MyData
    {
        public DateTime Category { get; set; }
        public int NetProfit { get; set; }
        public int StuPid { get; set; }
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

    private string PointColor(MyData arg)
    {
        return arg.NetProfit > 5000 ? "#FF0000" : "#02DFDE";
    }

    private string Point2Color(MyData arg)
    {
        return "#02DFDE";
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

    private Task GridContainerMouseDown(MouseEventArgs arg)
    {
        logger.LogInformation(arg.ClientX + ", " + arg.ClientY);
        return Task.CompletedTask;
    }

    private Task GridContainerMouseMove(MouseEventArgs arg)
    {
        logger.LogInformation(arg.ClientX + ", " + arg.ClientY);
        return Task.CompletedTask;
    }
}