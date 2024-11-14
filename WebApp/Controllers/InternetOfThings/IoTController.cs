using System.Diagnostics;
using System.Net.Mime;
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
public class IoTController(IoTCircuitBreakerService circuitBreakerService, IIoTBusinessLayer businessLayer, IIotRequestQueue requestQueueHostedService, ILogger<IoTController> logger) : ControllerBase
{
    [HttpPost("add-record")]
    public async Task<IActionResult> AddRecord([FromForm] string deviceId, [FromForm] float value, [FromForm] SensorType sensorType)
    {
        var cancelToken = HttpContext.RequestAborted;
        var success = await circuitBreakerService.TryProcessRequest(async () =>
        {
            try
            {
                IoTRecord record = new IoTRecord(deviceId, value, sensorType);
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
            return StatusCode(429, string.Empty);
        }

        return Ok();
    }

    [HttpPost("compute-record")]
    public async Task<IActionResult> SummaryRecord([FromForm] DateTime startDate, [FromForm] DateTime endDate)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<IoTRecord> reorderRecords = new List<IoTRecord>();

        var cancelToken = HttpContext.RequestAborted;

        try
        {
            var cursors = businessLayer.Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate, cancelToken, model => model.SensorData);
            await foreach (var record in cursors)
            {
                reorderRecords.Add(record);
            }

            var totalValue = reorderRecords.Sum(x => x.SensorData);
            var totalRecords = reorderRecords.Count;
            stopwatch.Stop();
            string result = $"Total Records: {totalRecords:N0} with value {totalValue:N0} in {stopwatch.ElapsedMilliseconds:N0} ms.";
            return Content(result, MediaTypeNames.Text.Plain);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(429, string.Empty);
        }
    }
}