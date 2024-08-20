using System.ComponentModel;
using BusinessModels.forecast;
using BusinessModels.Resources;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface IWeatherService
{
    [Description("Get the current weather in a given location")]
    public Task<WeatherModel> GetCurrentWeatherAsync([Description("The city and state, e.g. San Francisco, CA")] string location, [Description("Temperature unit. allowed Celsius, Fahrenheit")] Unit unit = Unit.Celsius, CancellationToken cancellationToken = default);
    
    
    
}