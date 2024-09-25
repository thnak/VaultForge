using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface IWeatherService
{
    [Description("get current weather in a given locatiion")]
    public Task<string> GetCurrentWeatherAsync([Description("The city and state, e.g. San Francisco, CA")] string location, CancellationToken cancellationToken = default);

    [Description("get forecast weather in a given location")]
    public Task<string> GetWeatherForecast([Description("The city and state, e.g. San Francisco, CA")] string location, [Description("Number of days of weather forecast. Value ranges from 1 to 14")] string days, CancellationToken cancellationToken = default);
    
    
    [Description("get location information in a given location like timespan, time zone, etc")]
    public Task<string> GetTimeZoneInfoAsync([Description("US Zipcode, UK Postcode, Canada Postalcode, IP address, Latitude/Longitude (decimal degree) or city name")] string location, CancellationToken cancellationToken = default);
}
