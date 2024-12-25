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
}