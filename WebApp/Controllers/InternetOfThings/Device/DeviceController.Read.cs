using System.Net.Mime;
using Business.Data.Repositories;
using BusinessModels.General.Results;
using BusinessModels.Resources;
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
        return Content(json, MediaTypeNames.Application.Json);
    }

    [HttpGet("check-available-ip")]
    public async Task<IActionResult> CheckAvailableIp(string ip)
    {
        var device = await deviceBusinessLayer.Where(x => x.IpAddress == ip).FirstOrDefault();
        var available = device == null;
        var result = Result<bool>.SuccessWithMessage(available, !available ? string.Format(AppLang.Has_been_used_by, device!.DeviceName) : AppLang.Available);
        return Content(result.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("check-available-mac")]
    public async Task<IActionResult> CheckAvailableMacAddress(string mac)
    {
        var device = await deviceBusinessLayer.Where(x => x.MacAddress == mac).FirstOrDefault();
        var available = device == null;
        var result = Result<bool>.SuccessWithMessage(available, !available ? string.Format(AppLang.Has_been_used_by, device!.DeviceName) : AppLang.Available);
        return Content(result.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("get-device-by-id")]
    public IActionResult GetDeviceByIdAsync(string deviceId)
    {
        var device = deviceBusinessLayer.Get(deviceId);
        return device == null ? NotFound() : Content(device.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("search-device")]
    public async Task<IActionResult> SearchDeviceAsync(string searchString)
    {
        var cancelToken = HttpContext.RequestAborted;
        var deviceCreateResult = deviceBusinessLayer.Search(searchString, 10, cancelToken);

        List<IoTDevice> result = [];
        await foreach (var de in deviceCreateResult) result.Add(de);

        result = [..result.DistinctBy(x => x.DeviceId)];

        var json = result.ToJson();
        return Content(json, MediaTypeNames.Application.Json);
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
        return Content(json, MediaTypeNames.Application.Json);
    }

    [HttpGet("get-sensor-by-id")]
    public IActionResult GetSensorByIdAsync(string id)
    {
        var device = sensorBusinessLayer.Get(id);
        return device == null ? NotFound() : Content(device.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("get-sensor-by-device-id")]
    public async Task<IActionResult> GetDeviceByDeviceIdAsync(string deviceId)
    {
        var device = sensorBusinessLayer.Where(x => x.DeviceId == deviceId);
        List<IoTSensor> result = [];
        await foreach (var d in device) result.Add(d);

        return Content(result.ToJson(), MediaTypeNames.Application.Json);
    }

    [HttpGet("search-sensor")]
    public async Task<IActionResult> SearchSensorAsync(string searchString)
    {
        var cancelToken = HttpContext.RequestAborted;
        var deviceCreateResult = sensorBusinessLayer.Where(x => x.SensorName == searchString || x.SensorId == searchString, cancelToken);

        List<IoTSensor> result = [];
        await foreach (var de in deviceCreateResult) result.Add(de);

        var json = result.ToJson();
        return Content(json, MediaTypeNames.Application.Json);
    }
}