using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.forecast;

public class ForecastWeatherModel
{
    [JsonPropertyName("forecast")]
    [JsonDescription("Forecast data")]
    public ForecastModel Forecast { get; set; } = new();
}