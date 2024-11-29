using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings;

public partial class IoTController
{
    [HttpPost("add-record")]
    public async Task<IActionResult> AddRecord([FromForm] string deviceId, [FromForm] string sensorId, [FromForm] float value, [FromForm] IoTSensorType ioTSensorType)
    {
        var cancelToken = HttpContext.RequestAborted;
        var success = await circuitBreakerService.TryProcessRequest(async token =>
        {
            try
            {
                IoTRecord record = new IoTRecord(deviceId, sensorId, value, ioTSensorType);
                var queueResult = await requestQueueHostedService.QueueRequest(record, token);
                if (!queueResult)
                {
                    logger.LogWarning($"Error while processing request {deviceId}");
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