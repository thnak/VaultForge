using BusinessModels.Resources;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ResApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(ILogger<WeatherForecastController> logger, IMemoryCache memoryCache) : ControllerBase
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger = logger;

    [HttpGet("index")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetIndex()
    {
        return Ok(AppLang.Hello);
    }

    [HttpGet("GetWeatherForecast")]
    [IgnoreAntiforgeryToken]
    [Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme}, {JwtBearerDefaults.AuthenticationScheme}")]
    public WeatherForecast[] Get()
    {
        return memoryCache.GetOrCreate("api/WeatherForecast/GetWeatherForecast", entry =>
        {
            return Enumerable.Range(1, 50).Select(index => new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();
        }) ?? [];
    }
}