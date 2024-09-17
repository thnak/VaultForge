using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.forecast;

public class ForecastModel
{
    [JsonPropertyName("forecastday")]
    [JsonDescription("List of forecast days")]
    public List<ForecastDayModel> ForecastDays { get; set; } = new();
}