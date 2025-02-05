﻿using System.Text.Json.Serialization;
using BusinessModels.Attribute;

namespace BusinessModels.forecast;

public class WeatherModel
{
    [JsonPropertyName("location")]
    [JsonDescription("Location details of the weather report")]
    public LocationModel Location { get; set; } = new();

    [JsonPropertyName("current")]
    [JsonDescription("Current weather conditions")]
    public CurrentModel Current { get; set; } = new();
}