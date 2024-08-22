using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.forecast;

public class CurrentModel
{
    [JsonPropertyName("last_updated_epoch")]
    [JsonDescription("Local time when the real time data was updated in unix time")]
    public long LastUpdatedEpoch { get; set; }

    [JsonPropertyName("last_updated")]
    [JsonDescription("Local time when the real time data was updated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("temp_f")]
    [JsonDescription("Temperature in celsius")]
    public float TempC { get; set; }

    [JsonPropertyName("temp_c")]
    [JsonDescription("")]
    public float TempF { get; set; }

    [JsonPropertyName("is_day")]
    [JsonDescription("1 = Yes 0 = No. Whether to show day condition icon or night icon")]
    public int IsDay { get; set; }


    [JsonPropertyName("wind_mph")]
    [JsonDescription("Wind speed in miles per hour")]
    public float WindMph { get; set; }


    [JsonPropertyName("wind_kph")]
    [JsonDescription("Wind speed in kilometer per hour")]
    public float WindKph { get; set; }


    [JsonPropertyName("wind_degree")]
    [JsonDescription("Wind direction in degrees")]
    public int WindDegree { get; set; }

    [JsonPropertyName("wind_wind_dir")]
    [JsonDescription("Wind direction as 16 point compass. e.g.: NSW")]
    public float WindDir { get; set; }


    [JsonPropertyName("pressure_mb")]
    [JsonDescription("Pressure in millibars")]
    public float PressureMb { get; set; }

    [JsonPropertyName("pressure_in")]
    [JsonDescription("Pressure in inches")]
    public float PressureIn { get; set; }

    [JsonPropertyName("precip_mm")]
    [JsonDescription("Precipitation amount in millimeters")]
    public float PrecipitationMm { get; set; }

    [JsonPropertyName("precip_in")]
    [JsonDescription("Precipitation amount in inches")]
    public float PrecipitationInches { get; set; }


    [JsonPropertyName("humidity")]
    [JsonDescription("Humidity as percentage")]
    public int Humidity { get; set; }


    [JsonPropertyName("cloud")]
    [JsonDescription("Cloud cover as percentage")]
    public int Cloud { get; set; }

    [JsonPropertyName("feelslike_c")]
    [JsonDescription("Feels like temperature in celsius")]
    public float FeelsLikeC { get; set; }

    [JsonPropertyName("feelslike_f")]
    [JsonDescription("Feels like temperature in fahrenheit")]
    public float FeelsLikeF { get; set; }


    [JsonPropertyName("windchill_c")]
    [JsonDescription("Windchill temperature in celcius")]
    public float WindchillC { get; set; }

    [JsonPropertyName("windchill_f")]
    [JsonDescription("Windchill temperature in fahrenheit")]
    public float WindchillF { get; set; }


    [JsonPropertyName("heatindex_c")]
    [JsonDescription("Heat index in celcius")]
    public float HeatIndexC { get; set; }

    [JsonPropertyName("heatindex_f")]
    [JsonDescription("Heat index in fahrenheit")]
    public float HeatIndexF { get; set; }


    [JsonPropertyName("dewpoint_c")]
    [JsonDescription("Dew point in celcius")]
    public float DewPointC { get; set; }

    [JsonPropertyName("dewpoint_f")]
    [JsonDescription("Dew point in fahrenheit")]
    public float DewPointF { get; set; }


    [JsonPropertyName("vis_km")]
    [JsonDescription("Visibility in kilometer")]
    public float VisMiles { get; set; }


    [JsonPropertyName("uv")]
    [JsonDescription("UV Index")]
    public float Uv { get; set; }

    [JsonPropertyName("gust_mph")]
    [JsonDescription("Wind gust in miles per hour")]
    public float GustMph { get; set; }


    [JsonPropertyName("gust_kph")]
    [JsonDescription("Wind gust in kilometer per hour")]
    public float GustKph { get; set; }
}