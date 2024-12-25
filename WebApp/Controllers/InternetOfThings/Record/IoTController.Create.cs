using BusinessModels.System.FileSystem;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Record;

public partial class IoTController
{
    [HttpPost("add-record")]
    public async Task<IActionResult> AddRecord([FromForm] string sensorId, [FromForm] float value, [FromForm] int? signalStrength, [FromForm] int? battery)
    {
        var cancelToken = HttpContext.RequestAborted;
        var success = await circuitBreakerService.TryProcessRequest(async token =>
        {
            try
            {
                IoTRecord record = new IoTRecord(new RecordMetadata()
                {
                    SensorId = sensorId,
                    SensorData = value,
                    SignalStrength = signalStrength ?? 0,
                    BatteryLevel = battery ?? 0,
                });
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

    [HttpPost("add-image")]
    public async Task<IActionResult> AddImage([FromForm] string sensorId, [FromForm] IFormFile file, [FromForm] int? signalStrength, [FromForm] int? battery, [FromForm] int? chipTemp)
    {
        var cancelToken = HttpContext.RequestAborted;
        var fileData = file.OpenReadStream();

        var folder = folderServe.Get("", "/iotImage");

        var fileInfo = new FileInfoModel()
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
        };
        var createFileResult = await folderServe.CreateFileAsync(folder!, fileInfo, cancelToken);
        if (!createFileResult.Item1)
            return BadRequest(createFileResult.Item2);

        await raidService.WriteDataAsync(fileData, fileInfo.AbsolutePath, cancelToken);
        await thumbnailService.AddThumbnailRequest(fileInfo.Id.ToString());
        IoTRecord record = new IoTRecord(new RecordMetadata()
        {
            SensorId = sensorId,
            SensorData = 0,
            SignalStrength = signalStrength ?? 0,
            BatteryLevel = battery ?? 0,
            OnChipTemperature = chipTemp ?? 0,
            ImagePath = fileInfo.AliasCode,
        });
        var queueResult = await requestQueueHostedService.QueueRequest(record, cancelToken);
        if (!queueResult)
        {
            await fileSystemServe.DeleteAsync(fileInfo.Id.ToString(), cancelToken);
            logger.LogWarning($"Error while processing request {sensorId}");
        }

        return Ok();
    }
}