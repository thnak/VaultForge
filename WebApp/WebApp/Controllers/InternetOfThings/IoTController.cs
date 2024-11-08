using Business.Business.Interfaces.InternetOfThings;
using Business.Services.Http.CircuitBreakers;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Route("api/[controller]")]
[ApiController]
public class IoTController(IoTCircuitBreakerService circuitBreakerService, IIotRequestQueue requestQueueHostedService, ILogger<IoTController> logger) : ControllerBase
{
    [HttpPost("add-record")]
    public async Task<IActionResult> AddRecord([FromForm] string deviceId, [FromForm] float value)
    {
        var cancelToken = HttpContext.RequestAborted;
        var success = await circuitBreakerService.TryProcessRequest(async () =>
        {
            try
            {
                IoTRecord record = new IoTRecord(deviceId, value);
                var queueResult = await requestQueueHostedService.QueueRequest(record, cancelToken);
                if (!queueResult)
                {
                    logger.LogWarning($"Error while processing request {deviceId}");
                }
            }
            catch (OperationCanceledException)
            {
                //
            }
        });
        if (!success)
        {
            logger.LogWarning("Server is overloaded, try again later.");
            return StatusCode(429, "Server is overloaded, try again later.");
        }

        return Ok("Request processed successfully.");
    }
}