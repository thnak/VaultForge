using Business.Services.OllamaToolCallingServices.Interfaces;
using BusinessModels.forecast;
using BusinessModels.Resources;

namespace Business.Services.OllamaToolCallingServices;

public class WeatherService : IWeatherService
{
    public Task<WeatherModel> GetCurrentWeatherAsync(string location, Unit unit = Unit.Celsius, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WeatherModel()
        {
            Location = location,
            Temperature = 22.0,
            Unit = unit,
            Description = "Sunny",
        });
    }
}