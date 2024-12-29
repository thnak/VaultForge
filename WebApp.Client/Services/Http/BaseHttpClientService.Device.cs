using System.Web;
using BusinessModels.General.Results;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<ResponseDataResult<SignalrResultValue<IoTDevice>>> GetAllDevicesAsync(CancellationToken cancellationToken = default)
    {
        int pageSize = int.MaxValue;
        var result = await GetAsync<SignalrResultValue<IoTDevice>>($"api/device/get-device?page=0&pageSize={pageSize}", cancellationToken);
        return result;
    }

    public async Task<bool> CheckIfDeviceExists(string deviceId, CancellationToken cancellationToken = default)
    {
        deviceId = HttpUtility.UrlEncode(deviceId);
        var result = await GetAsync<IoTDevice>($"/api/device/get-device-by-id?deviceId={deviceId}", cancellationToken);
        return result.Data != null;
    }
    
    public async Task<ResponseDataResult<Result<bool>>> CheckAvailableMacAddress(string mac, CancellationToken cancellationToken = default)
    {
        mac = HttpUtility.UrlEncode(mac);
        var result = await GetAsync<Result<bool>>($"api/Device/check-available-mac?mac={mac}", cancellationToken);
        return result;
    }

    public async Task<ResponseDataResult<Result<bool>>> CheckAvailableIpAddress(string ip, CancellationToken cancellationToken = default)
    {
        ip = HttpUtility.UrlEncode(ip);
        var result = await GetAsync<Result<bool>>($"api/Device/check-available-ip?ip={ip}", cancellationToken);
        return result;
    }
}