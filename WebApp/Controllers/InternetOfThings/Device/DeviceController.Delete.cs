using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Device;

public partial class DeviceController
{
    [HttpPost("delete-device")]
    public async Task<IActionResult> DeleteDeviceAsync([FromForm] string deviceId)
    {
        var cancelToken = HttpContext.RequestAborted;

        var deviceDeleteResult = await deviceBusinessLayer.DeleteAsync(deviceId, cancelToken);
        return Ok(deviceDeleteResult.ToJson());
    }

    [HttpPost("delete-sensor")]
    public async Task<IActionResult> DeleteSensorAsync([FromForm] string sensorId)
    {
        var cancelToken = HttpContext.RequestAborted;

        var createResult = await sensorBusinessLayer.DeleteAsync(sensorId, cancelToken);
        return Ok(createResult.ToJson());
    }
}