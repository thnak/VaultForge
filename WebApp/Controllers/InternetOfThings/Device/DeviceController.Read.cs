using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers.InternetOfThings.Device;

public partial class DeviceController
{
    [HttpGet("get-device")]
    public async Task<IActionResult> GetDeviceAsync(int page, int pageSize)
    {
        var cancelToken = HttpContext.RequestAborted;
        var deviceCreateResult = await deviceBusinessLayer.GetAllAsync(page, pageSize, cancelToken);
        SignalrResultValue<IoTDevice> result = new()
        {
            Data = deviceCreateResult.Item1,
            Total = deviceCreateResult.Item2
        };
        var json = result.ToJson();
        return Content(json, "application/json");
    }

    [HttpGet("get-device-by-id")]
    public IActionResult GetDeviceByIdAsync(string deviceId)
    {
        var device = deviceBusinessLayer.Get(deviceId);
        return device == null ? NotFound() : Content(device.ToJson(), "application/json");
    }

    [HttpGet("search-device")]
    public async Task<IActionResult> SearchDeviceAsync(string searchString)
    {
        var cancelToken = HttpContext.RequestAborted;
        var deviceCreateResult = deviceBusinessLayer.Where(x => x.DeviceName == searchString || x.MacAddress == searchString || x.IpAddress == searchString, cancelToken);

        List<IoTDevice> result = [];
        await foreach (var de in deviceCreateResult)
        {
            result.Add(de);
        }

        var json = result.ToJson();
        return Content(json, "application/json");
    }


    [HttpGet("get-sensor")]
    public async Task<IActionResult> GetSensorAsync(int page, int pageSize)
    {
        var cancelToken = HttpContext.RequestAborted;
        var deviceCreateResult = await sensorBusinessLayer.GetAllAsync(page, pageSize, cancelToken);
        SignalrResultValue<IoTSensor> result = new()
        {
            Data = deviceCreateResult.Item1,
            Total = deviceCreateResult.Item2
        };
        var json = result.ToJson();
        return Content(json, "application/json");
    }
    
    [HttpGet("get-sensor-by-id")]
    public IActionResult GetSensorByIdAsync(string id)
    {
        var device = sensorBusinessLayer.Get(id);
        return device == null ? NotFound() : Content(device.ToJson(), "application/json");
    }

    [HttpGet("get-sensor-by-device-id")]
    public async Task<IActionResult> GetDeviceByDeviceIdAsync(string deviceId)
    {
        var device = sensorBusinessLayer.Where(x=>x.DeviceId == deviceId);
        List<IoTSensor> result = [];
        await foreach (var d in device)
        {
            result.Add(d);
        }
        
        return Content(result.ToJson(), "application/json");
    }
    
    [HttpGet("search-sensor")]
    public async Task<IActionResult> SearchSensorAsync(string searchString)
    {
        var cancelToken = HttpContext.RequestAborted;
        var deviceCreateResult = sensorBusinessLayer.Where(x => x.SensorName == searchString || x.SensorId == searchString, cancelToken);

        List<IoTSensor> result = [];
        await foreach (var de in deviceCreateResult)
        {
            result.Add(de);
        }

        var json = result.ToJson();
        return Content(json, "application/json");
    }
}