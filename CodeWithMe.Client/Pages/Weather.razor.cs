using Microsoft.AspNetCore.Components;

namespace CodeWithMe.Client.Pages;

public partial class Weather(PersistentComponentState state) : ComponentBase, IDisposable
{
    private PersistingComponentStateSubscription _subscription;

    private WeatherForecast[]? forecasts;

    protected override async Task OnInitializedAsync()
    {
        // Simulate asynchronous loading to demonstrate streaming rendering
        await Task.Delay(500);

        state.RegisterOnPersisting(Persist);
        var has = state.TryTakeFromJson("myWheather", out WeatherForecast[] data);

        forecasts = has ? data : GetData();
    }

    private Task Persist()
    {
        state.PersistAsJson("myWheather", forecasts);
        return Task.CompletedTask;
    }

    private WeatherForecast[] GetData()
    {
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = startDate.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        }).ToArray();
    }

    private class WeatherForecast
    {
        public DateOnly Date { get; set; }
        public int TemperatureC { get; set; }
        public string? Summary { get; set; }
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}