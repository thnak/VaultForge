using BusinessModels.General.Results;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<ResponseDataResult<SignalrResultValue<IoTSensor>>> GetAllIotSensorsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<SignalrResultValue<IoTSensor>>($"api/device/get-sensor?page={page}&pageSize={pageSize}", cancellationToken);
        return result;
    }

    public async Task<List<IoTSensor>> GetIotSensorFromDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<List<IoTSensor>>("api/Device/get-sensor-by-device-id?deviceId=" + deviceId, cancellationToken);
        if(result.IsSuccessStatusCode)
            return result.Data ?? [];
        
        Logger.LogWarning(result.Message);
        return [];
    }
}