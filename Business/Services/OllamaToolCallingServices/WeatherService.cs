using System.Text.Json;
using Business.Services.OllamaToolCallingServices.Interfaces;
using BusinessModels.Converter;
using BusinessModels.forecast;
using BusinessModels.Resources;
using BusinessModels.Utils;

namespace Business.Services.OllamaToolCallingServices;

public class WeatherService(string baseUri) : IWeatherService
{
    private HttpClient Http { get; set; } = new() { BaseAddress = new Uri(baseUri) };

    public async Task<string> GetCurrentWeatherAsync(string location, Unit unit = Unit.Celsius, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"v1/current.json?key=27749193cede47088b880523242208&q={location}", null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var textPlan = await response.Content.ReadAsStringAsync(cancellationToken);
            var weather = textPlan.DeSerialize<WeatherModel>();
            if (weather != default)
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonDescriptionConverter<WeatherModel>() },
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(weather, options);
                return json;
            }
        }

        return "failed to fetch data";
    }
}