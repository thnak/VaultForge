using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Device;

public partial class DeviceController
{
    [HttpPost("add-new-device")]
    public async Task<IActionResult> CreateDeviceAsync([FromForm] RequestToCreate requestDevice)
    {
        var cancelToken = HttpContext.RequestAborted;

        var deviceCreateResult = await deviceBusinessLayer.CreateAsync(requestDevice.Device, cancelToken);
        if (deviceCreateResult.IsSuccess)
        {
            foreach (var sensor in requestDevice.Sensors)
            {
                await sensorBusinessLayer.CreateAsync(sensor, cancelToken);
            }

            return Ok(deviceCreateResult.Message);
        }

        return BadRequest(deviceCreateResult.Message);
    }

    [HttpPost("add-new-sensor")]
    public async Task<IActionResult> CreateSensorAsync([FromForm] string deviceId, [FromForm] IoTSensor sensor)
    {
        var cancelToken = HttpContext.RequestAborted;

        var device = deviceBusinessLayer.Get(deviceId);
        if (device == null) return BadRequest(AppLang.Device_not_found);

        var createResult = await sensorBusinessLayer.CreateAsync(sensor, cancelToken);
        return createResult.IsSuccess ? Ok(createResult.Message) : BadRequest(createResult.Message);
    }
}