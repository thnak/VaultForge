using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings;

public partial class IoTController
{
    [HttpPost("add-record")]
    public async Task<IActionResult> AddRecord([FromForm] string sensorId, [FromForm] float value)
    {
        var cancelToken = HttpContext.RequestAborted;
        var success = await circuitBreakerService.TryProcessRequest(async token =>
        {
            try
            {
                IoTRecord record = new IoTRecord(sensorId, value);
                var queueResult = await requestQueueHostedService.QueueRequest(record, token);
                if (!queueResult)
                {
                    logger.LogWarning($"Error while processing request {sensorId}");
                }
            }
            catch (OperationCanceledException)
            {
                //
            }
        }, cancelToken);
        if (!success.IsSuccess)
        {
            logger.LogWarning("Server is overloaded, try again later.");
            return StatusCode(429, string.Empty);
        }

        return Ok();
    }
}