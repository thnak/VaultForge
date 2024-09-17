using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.forecast;

public class ForecastDayModel
{
    [JsonPropertyName("date")]
    [JsonDescription("Date in yyyy-MM-dd format")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("date_epoch")]
    [JsonDescription("Date in Unix time")]
    public long DateEpoch { get; set; }

    [JsonPropertyName("day")]
    [JsonDescription("Day forecast data")]
    public DayModel Day { get; set; } = new();

    [JsonPropertyName("astro")]
    [JsonDescription("Astronomical data")]
    public AstroModel Astro { get; set; } = new();

    [JsonPropertyName("hour")]
    [JsonDescription("Hourly forecast data")]
    public List<HourModel> HourlyForecast { get; set; } = new();
}

public class DayModel
{
    [JsonPropertyName("maxtemp_c")]
    [JsonDescription("Maximum temperature in Celsius")]
    public double MaxTempC { get; set; }

    [JsonPropertyName("maxtemp_f")]
    [JsonDescription("Maximum temperature in Fahrenheit")]
    public double MaxTempF { get; set; }

    [JsonPropertyName("mintemp_c")]
    [JsonDescription("Minimum temperature in Celsius")]
    public double MinTempC { get; set; }

    [JsonPropertyName("mintemp_f")]
    [JsonDescription("Minimum temperature in Fahrenheit")]
    public double MinTempF { get; set; }

    [JsonPropertyName("avgtemp_c")]
    [JsonDescription("Average temperature in Celsius")]
    public double AvgTempC { get; set; }

    [JsonPropertyName("avgtemp_f")]
    [JsonDescription("Average temperature in Fahrenheit")]
    public double AvgTempF { get; set; }

    [JsonPropertyName("maxwind_mph")]
    [JsonDescription("Maximum wind speed in miles per hour")]
    public double MaxWindMph { get; set; }

    [JsonPropertyName("maxwind_kph")]
    [JsonDescription("Maximum wind speed in kilometers per hour")]
    public double MaxWindKph { get; set; }

    [JsonPropertyName("totalprecip_mm")]
    [JsonDescription("Total precipitation in millimeters")]
    public double TotalPrecipMm { get; set; }

    [JsonPropertyName("totalprecip_in")]
    [JsonDescription("Total precipitation in inches")]
    public double TotalPrecipIn { get; set; }

    [JsonPropertyName("totalsnow_cm")]
    [JsonDescription("Total snowfall in centimeters")]
    public double TotalSnowCm { get; set; }

    [JsonPropertyName("avgvis_km")]
    [JsonDescription("Average visibility in kilometers")]
    public double AvgVisKm { get; set; }

    [JsonPropertyName("avgvis_miles")]
    [JsonDescription("Average visibility in miles")]
    public double AvgVisMiles { get; set; }

    [JsonPropertyName("avghumidity")]
    [JsonDescription("Average humidity as percentage")]
    public int AvgHumidity { get; set; }

    [JsonPropertyName("daily_will_it_rain")]
    [JsonDescription("Will it rain? 1 = Yes, 0 = No")]
    public int DailyWillItRain { get; set; }

    [JsonPropertyName("daily_chance_of_rain")]
    [JsonDescription("Chance of rain in percentage")]
    public int DailyChanceOfRain { get; set; }

    [JsonPropertyName("daily_will_it_snow")]
    [JsonDescription("Will it snow? 1 = Yes, 0 = No")]
    public int DailyWillItSnow { get; set; }

    [JsonPropertyName("daily_chance_of_snow")]
    [JsonDescription("Chance of snow in percentage")]
    public int DailyChanceOfSnow { get; set; }

    [JsonPropertyName("condition")]
    [JsonDescription("Condition details")]
    public WeatherCondition Condition { get; set; } = new();

    [JsonPropertyName("uv")]
    [JsonDescription("UV Index")]
    public double UvIndex { get; set; }
}

public class AstroModel
{
    [JsonPropertyName("sunrise")]
    [JsonDescription("Sunrise time")]
    public string Sunrise { get; set; } = string.Empty;

    [JsonPropertyName("sunset")]
    [JsonDescription("Sunset time")]
    public string Sunset { get; set; } = string.Empty;

    [JsonPropertyName("moonrise")]
    [JsonDescription("Moonrise time")]
    public string Moonrise { get; set; } = string.Empty;

    [JsonPropertyName("moonset")]
    [JsonDescription("Moonset time")]
    public string Moonset { get; set; } = string.Empty;

    [JsonPropertyName("moon_phase")]
    [JsonDescription("Moon phase")]
    public string MoonPhase { get; set; } = string.Empty;

    [JsonPropertyName("moon_illumination")]
    [JsonDescription("Moon illumination percentage")]
    public int MoonIllumination { get; set; }

    [JsonPropertyName("is_moon_up")]
    [JsonDescription("Is the moon up? 1 = Yes, 0 = No")]
    public int IsMoonUp { get; set; }

    [JsonPropertyName("is_sun_up")]
    [JsonDescription("Is the sun up? 1 = Yes, 0 = No")]
    public int IsSunUp { get; set; }
}

public class HourModel
{
    [JsonPropertyName("time_epoch")]
    [JsonDescription("Time in Unix format")]
    public long TimeEpoch { get; set; }

    [JsonPropertyName("time")]
    [JsonDescription("Time in yyyy-MM-dd HH:mm format")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temp_c")]
    [JsonDescription("Temperature in Celsius")]
    public double TempC { get; set; }

    [JsonPropertyName("temp_f")]
    [JsonDescription("Temperature in Fahrenheit")]
    public double TempF { get; set; }

    [JsonPropertyName("is_day")]
    [JsonDescription("1 = Day, 0 = Night")]
    public int IsDay { get; set; }

    [JsonPropertyName("condition")]
    [JsonDescription("Condition details")]
    public WeatherCondition Condition { get; set; } = new();

    [JsonPropertyName("wind_mph")]
    [JsonDescription("Wind speed in miles per hour")]
    public double WindMph { get; set; }

    [JsonPropertyName("wind_kph")]
    [JsonDescription("Wind speed in kilometers per hour")]
    public double WindKph { get; set; }

    [JsonPropertyName("wind_degree")]
    [JsonDescription("Wind direction in degrees")]
    public int WindDegree { get; set; }

    [JsonPropertyName("wind_dir")]
    [JsonDescription("Wind direction as compass point")]
    public string WindDir { get; set; } = string.Empty;

    [JsonPropertyName("pressure_mb")]
    [JsonDescription("Pressure in millibars")]
    public double PressureMb { get; set; }

    [JsonPropertyName("pressure_in")]
    [JsonDescription("Pressure in inches")]
    public double PressureIn { get; set; }

    [JsonPropertyName("precip_mm")]
    [JsonDescription("Precipitation in millimeters")]
    public double PrecipMm { get; set; }

    [JsonPropertyName("precip_in")]
    [JsonDescription("Precipitation in inches")]
    public double PrecipIn { get; set; }

    [JsonPropertyName("snow_cm")]
    [JsonDescription("Snowfall in centimeters")]
    public double SnowCm { get; set; }

    [JsonPropertyName("humidity")]
    [JsonDescription("Humidity as percentage")]
    public int Humidity { get; set; }

    [JsonPropertyName("cloud")]
    [JsonDescription("Cloud cover as percentage")]
    public int Cloud { get; set; }

    [JsonPropertyName("feelslike_c")]
    [JsonDescription("Feels like temperature in Celsius")]
    public double FeelsLikeC { get; set; }

    [JsonPropertyName("feelslike_f")]
    [JsonDescription("Feels like temperature in Fahrenheit")]
    public double FeelsLikeF { get; set; }

    [JsonPropertyName("windchill_c")]
    [JsonDescription("Windchill temperature in Celsius")]
    public double WindChillC { get; set; }

    [JsonPropertyName("windchill_f")]
    [JsonDescription("Windchill temperature in Fahrenheit")]
    public double WindChillF { get; set; }

    [JsonPropertyName("heatindex_c")]
    [JsonDescription("Heat index in Celsius")]
    public double HeatIndexC { get; set; }

    [JsonPropertyName("heatindex_f")]
    [JsonDescription("Heat index in Fahrenheit")]
    public double HeatIndexF { get; set; }

    [JsonPropertyName("dewpoint_c")]
    [JsonDescription("Dew point in Celsius")]

    public double DewPointC { get; set; }

    [JsonPropertyName("dewpoint_f")]
    [JsonDescription("Dew point in Fahrenheit")]
    public double DewPointF { get; set; }

    [JsonPropertyName("will_it_rain")]
    [JsonDescription("Will it rain? 1 = Yes, 0 = No")]
    public int WillItRain { get; set; }

    [JsonPropertyName("chance_of_rain")]
    [JsonDescription("Chance of rain in percentage")]
    public int ChanceOfRain { get; set; }

    [JsonPropertyName("will_it_snow")]
    [JsonDescription("Will it snow? 1 = Yes, 0 = No")]
    public int WillItSnow { get; set; }

    [JsonPropertyName("chance_of_snow")]
    [JsonDescription("Chance of snow in percentage")]
    public int ChanceOfSnow { get; set; }

    [JsonPropertyName("vis_km")]
    [JsonDescription("Visibility in kilometers")]
    public double VisibilityKm { get; set; }

    [JsonPropertyName("vis_miles")]
    [JsonDescription("Visibility in miles")]
    public double VisibilityMiles { get; set; }

    [JsonPropertyName("gust_mph")]
    [JsonDescription("Wind gust speed in miles per hour")]
    public double GustMph { get; set; }

    [JsonPropertyName("gust_kph")]
    [JsonDescription("Wind gust speed in kilometers per hour")]
    public double GustKph { get; set; }

    [JsonPropertyName("uv")]
    [JsonDescription("UV Index")]
    public double UVIndex { get; set; }
}