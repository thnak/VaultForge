using System.Text.Json.Serialization;

namespace BusinessModels.forecast;

public abstract class WeatherModel
{
    [JsonPropertyName("location")] public LocationModel Location { get; set; } = new();
    [JsonPropertyName("current")] public CurrentModel Current { get; set; } = new();
}