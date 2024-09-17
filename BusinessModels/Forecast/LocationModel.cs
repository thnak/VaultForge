using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.forecast;

public class LocationModel
{
    [JsonPropertyName("name")]
    [JsonDescription("Location name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    [JsonDescription("Region or state of the location, if available")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    [JsonDescription("Location country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    [JsonDescription("Latitude in decimal degree")]
    public double Lat { get; set; }


    [JsonPropertyName("lon")]
    [JsonDescription("Longitude in decimal degree")]
    public double Lon { get; set; }

    [JsonPropertyName("tz_id")]
    [JsonDescription("Time zone ID")]
    public string TimeZoneId { get; set; } = string.Empty;

    [JsonPropertyName("localtime_epoch")]
    [JsonDescription("Local date and time in unix time")]
    public long LocaltimeEpoch { get; set; }

    [JsonPropertyName("localtime")]
    [JsonDescription("Local date and time")]
    public string Localtime { get; set; } = string.Empty;
}