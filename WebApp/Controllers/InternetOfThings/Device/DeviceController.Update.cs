using BusinessModels.General.Update;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace WebApp.Controllers.InternetOfThings.Device;

public partial class DeviceController
{
    [HttpPost("update-device")]
    public async Task<IActionResult> UpdateDeviceAsync([FromForm] string deviceId, [FromForm] string json)
    {
        var cancelToken = HttpContext.RequestAborted;

        var field2Update = new FieldUpdate<IoTDevice>();
        field2Update.SetFromJson(json);

        var deviceUpdateResult = await deviceBusinessLayer.UpdateAsync(deviceId, field2Update, cancelToken);
        return Ok(deviceUpdateResult.ToJson());
    }

    [HttpPost("update-sensor")]
    public async Task<IActionResult> UpdateSensorAsync([FromForm] string sensorId, [FromForm] string json)
    {
        var cancelToken = HttpContext.RequestAborted;

        var field2Update = new FieldUpdate<IoTSensor>();
        field2Update.SetFromJson(json);

        var deviceUpdateResult = await sensorBusinessLayer.UpdateAsync(sensorId, field2Update, cancelToken);
        return Ok(deviceUpdateResult.ToJson());
    }
}