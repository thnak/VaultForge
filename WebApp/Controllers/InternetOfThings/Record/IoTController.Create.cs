using BusinessModels.Resources;
using BusinessModels.System.FileSystem;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Record;

public partial class IoTController
{
    [HttpPost("add-record")]
    public async Task<IActionResult> AddRecord([FromForm] string sensorId, [FromForm] float value, [FromForm] int? signalStrength, [FromForm] int? battery, [FromForm] DateTime? dateTime)
    {
        var cancelToken = HttpContext.RequestAborted;
        var success = await circuitBreakerService.TryProcessRequest(async token =>
        {
            try
            {
                var record = new IoTRecord(new RecordMetadata
                {
                    SensorId = sensorId,
                    SensorData = value,
                    SignalStrength = signalStrength ?? 0,
                    BatteryLevel = battery ?? 0,
                    RecordedAt = dateTime ?? timeProvider.UtcNow()
                });
                var queueResult = await requestQueueHostedService.QueueRequest(record, token);
                if (!queueResult) logger.LogWarning($"{AppLang.Error_processing_request} {sensorId}");
            }
            catch (OperationCanceledException)
            {
                //
            }
        }, cancelToken);
        if (!success.IsSuccess)
        {
            logger.LogWarning(AppLang.Server_overloaded_try_again_later);
            return StatusCode(429, string.Empty);
        }

        return Ok();
    }

    [HttpPost("add-image")]
    public async Task<IActionResult> AddImage([FromForm] string sensorId, [FromForm] IFormFile file, [FromForm] int? signalStrength, [FromForm] int? battery, [FromForm] int? chipTemp, [FromForm] DateTime? dateTime)
    {
        var cancelToken = HttpContext.RequestAborted;
        try
        {
            await using var fileData = file.OpenReadStream();

            var folder = folderServe.Get("", "/iotImage");

            var fileInfo = new FileInfoModel
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length
            };
            var createFileResult = await folderServe.CreateFileAsync(folder!, fileInfo, cancelToken);
            if (!createFileResult.IsSuccess)
                return BadRequest(createFileResult.Message);

            await raidService.WriteDataAsync(fileData, fileInfo.AbsolutePath, cancelToken);
            var record = new IoTRecord(new RecordMetadata
            {
                SensorId = sensorId,
                SensorData = 0,
                SignalStrength = signalStrength ?? 0,
                BatteryLevel = battery ?? 0,
                OnChipTemperature = chipTemp ?? 0,
                ImagePath = fileInfo.AliasCode,
                RecordedAt = dateTime ?? timeProvider.UtcNow()
            });
            var queueResult = await requestQueueHostedService.QueueRequest(record, cancelToken);
            if (!queueResult)
            {
                await fileSystemServe.DeleteAsync(fileInfo.Id.ToString(), cancelToken);
                logger.LogWarning($"{AppLang.Error_processing_request} {sensorId}");
            }

            return Ok();
        }
        catch (OperationCanceledException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("add-many-image")]
    public async Task<IActionResult> AddManyImage([FromForm] string sensorId, [FromForm] List<IFormFile> files, [FromForm] int? signalStrength, [FromForm] int? battery, [FromForm] int? chipTemp, [FromForm] DateTime? dateTime)
    {
        var cancelToken = HttpContext.RequestAborted;
        try
        {
            foreach (var file in files)
            {
                await using var fileData = file.OpenReadStream();
                var folder = folderServe.Get("", "/iotImage");

                var fileInfo = new FileInfoModel
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length
                };
                fileInfo.FileName = Guid.NewGuid() + ".jpg";
                var createFileResult = await folderServe.CreateFileAsync(folder!, fileInfo, cancelToken);
                if (!createFileResult.IsSuccess)
                    return BadRequest(createFileResult.Message);

                await raidService.WriteDataAsync(fileData, fileInfo.AbsolutePath, cancelToken);
                var record = new IoTRecord(new RecordMetadata
                {
                    SensorId = sensorId,
                    SensorData = 0,
                    SignalStrength = signalStrength ?? 0,
                    BatteryLevel = battery ?? 0,
                    OnChipTemperature = chipTemp ?? 0,
                    ImagePath = fileInfo.AliasCode,
                    RecordedAt = dateTime ?? timeProvider.UtcNow()
                });
                var queueResult = await requestQueueHostedService.QueueRequest(record, cancelToken);
                if (!queueResult)
                {
                    await fileSystemServe.DeleteAsync(fileInfo.Id.ToString(), cancelToken);
                    logger.LogWarning($"{AppLang.Error_processing_request} {sensorId}");
                }
            }

            return Ok();
        }
        catch (OperationCanceledException e)
        {
            return BadRequest(e.Message);
        }
    }
}