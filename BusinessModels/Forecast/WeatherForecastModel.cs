using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.forecast;

public class WeatherForecastModel
{
    [JsonPropertyName("location")]
    [JsonDescription("Location details of the weather report")]
    public LocationModel Location { get; set; } = new();

    [JsonPropertyName("current")]
    [JsonDescription("Current weather conditions")]
    public CurrentModel Current { get; set; } = new();
    
    [JsonPropertyName("forecast")]
    [JsonDescription("Forecast weather conditions")]
    public ForecastWeatherModel ForecastWeather { get; set; } = new();
}