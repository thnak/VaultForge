using System.ComponentModel;
using CSharpToJsonSchema;
using Ollama;

using var ollama = new OllamaApiClient();
var chat = ollama.Chat(
    model: "llama3.2",
    systemMessage: "You are a helpful weather assistant.",
    autoCallTools: true);

var service = new WeatherService();
chat.AddToolService(service.AsTools(), service.AsCalls());

try
{
    _ = await chat.SendAsync("What is the current temperature in Dubai, UAE in Celsius?");
}
finally
{
    Console.WriteLine(chat.PrintMessages());
}

public enum Unit
{
    Celsius,
    Fahrenheit,
}

public class Weather
{
    public string Location { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public Unit Unit { get; set; }
    public string Description { get; set; } = string.Empty;
}

[GenerateJsonSchema]
public interface IWeatherFunctions
{
    [Description("Get the current weather in a given location")]
    public Task<Weather> GetCurrentWeatherAsync(
        [Description("The city and state, e.g. San Francisco, CA")] string location,
        Unit unit = Unit.Celsius,
        CancellationToken cancellationToken = default);
}

public class WeatherService : IWeatherFunctions
{
    public Task<Weather> GetCurrentWeatherAsync(string location, Unit unit = Unit.Celsius, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Weather
        {
            Location = location,
            Temperature = 22.0,
            Unit = unit,
            Description = "Sunny",
        });
    }
}