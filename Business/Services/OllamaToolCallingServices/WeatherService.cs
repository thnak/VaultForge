using Business.Services.OllamaToolCallingServices.Interfaces;
using BusinessModels.forecast;
using BusinessModels.Utils;

namespace Business.Services.OllamaToolCallingServices;

public class WeatherService(string baseUri) : IWeatherService
{
    private HttpClient Http { get; set; } = new() { BaseAddress = new Uri(baseUri) };

    public async Task<string> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"v1/current.json?key=27749193cede47088b880523242208&q={location}&lang=en-US", null, cancellationToken);
        var textPlan = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var weather = textPlan.DeSerialize<WeatherModel>();
            if (weather != default)
            {
                string json = weather.ToJson<WeatherModel>();
                return json;
            }
        }

        return textPlan;
    }

    public async Task<string> GetWeatherForecast(string location, string days, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"v1/forecast.json?key=27749193cede47088b880523242208&q={location}&lang=en-US&days={days}", null, cancellationToken);
        var textPlan = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var weather = textPlan.DeSerialize<WeatherForecastModel>();
            if (weather != default)
            {
                string json = weather.ToJson<WeatherForecastModel>();
                return json;
            }
        }

        return textPlan;
    }

    public async Task<string> GetTimeZoneInfoAsync(string location, CancellationToken cancellationToken = default)
    {
        var response = await Http.PostAsync($"v1/timezone.json?key=27749193cede47088b880523242208&q={location}", null, cancellationToken);
        var textPlan = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var weather = textPlan.DeSerialize<LocationModel>();
            if (weather != default)
            {
                string json = weather.ToJson<LocationModel>();
                return json;
            }
        }

        return textPlan;
    }
}