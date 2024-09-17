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
    public double TempC { get; set; }

    [JsonPropertyName("temp_c")]
    [JsonDescription("")]
    public double TempF { get; set; }

    [JsonPropertyName("is_day")]
    [JsonDescription("1 = Yes 0 = No. Whether to show day condition icon or night icon")]
    public int IsDay { get; set; }


    [JsonPropertyName("condition")]
    [JsonDescription("Condition")]
    public WeatherCondition Condition { get; set; } = new();

    [JsonPropertyName("wind_mph")]
    [JsonDescription("Wind speed in miles per hour")]
    public double WindMph { get; set; }


    [JsonPropertyName("wind_kph")]
    [JsonDescription("Wind speed in kilometer per hour")]
    public double WindKph { get; set; }


    [JsonPropertyName("wind_degree")]
    [JsonDescription("Wind direction in degrees")]
    public int WindDegree { get; set; }

    [JsonPropertyName("wind_wind_dir")]
    [JsonDescription("Wind direction as 16 point compass. e.g.: NSW")]
    public double WindDir { get; set; }


    [JsonPropertyName("pressure_mb")]
    [JsonDescription("Pressure in millibars")]
    public double PressureMb { get; set; }

    [JsonPropertyName("pressure_in")]
    [JsonDescription("Pressure in inches")]
    public double PressureIn { get; set; }

    [JsonPropertyName("precip_mm")]
    [JsonDescription("Precipitation amount in millimeters")]
    public double PrecipitationMm { get; set; }

    [JsonPropertyName("precip_in")]
    [JsonDescription("Precipitation amount in inches")]
    public double PrecipitationInches { get; set; }


    [JsonPropertyName("humidity")]
    [JsonDescription("Humidity as percentage")]
    public int Humidity { get; set; }


    [JsonPropertyName("cloud")]
    [JsonDescription("Cloud cover as percentage")]
    public int Cloud { get; set; }

    [JsonPropertyName("feelslike_c")]
    [JsonDescription("Feels like temperature in celsius")]
    public double FeelsLikeC { get; set; }

    [JsonPropertyName("feelslike_f")]
    [JsonDescription("Feels like temperature in fahrenheit")]
    public double FeelsLikeF { get; set; }


    [JsonPropertyName("windchill_c")]
    [JsonDescription("Windchill temperature in celcius")]
    public double WindchillC { get; set; }

    [JsonPropertyName("windchill_f")]
    [JsonDescription("Windchill temperature in fahrenheit")]
    public double WindchillF { get; set; }


    [JsonPropertyName("heatindex_c")]
    [JsonDescription("Heat index in celcius")]
    public double HeatIndexC { get; set; }

    [JsonPropertyName("heatindex_f")]
    [JsonDescription("Heat index in fahrenheit")]
    public double HeatIndexF { get; set; }


    [JsonPropertyName("dewpoint_c")]
    [JsonDescription("Dew point in celcius")]
    public double DewPointC { get; set; }

    [JsonPropertyName("dewpoint_f")]
    [JsonDescription("Dew point in fahrenheit")]
    public double DewPointF { get; set; }


    [JsonPropertyName("vis_km")]
    [JsonDescription("Visibility in kilometer")]
    public double VisibilityKm { get; set; }

    [JsonDescription("Visibility in miler")]
    [JsonPropertyName("vis_miles")]
    public double VisibilityMiles { get; set; }


    [JsonPropertyName("uv")]
    [JsonDescription("UV Index")]
    public double UvIndex { get; set; }

    [JsonPropertyName("gust_mph")]
    [JsonDescription("Wind gust in miles per hour")]
    public double GustMph { get; set; }


    [JsonPropertyName("gust_kph")]
    [JsonDescription("Wind gust in kilometer per hour")]
    public double GustKph { get; set; }
}