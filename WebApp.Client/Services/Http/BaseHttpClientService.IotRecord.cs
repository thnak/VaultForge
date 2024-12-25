using BusinessModels.General.Results;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<ResponseDataResult<SignalrResultValue<IoTSensor>>> GetAllIotSensorsAsync(CancellationToken cancellationToken = default)
    {
        int pageSize = int.MaxValue;
        var result = await GetAsync<SignalrResultValue<IoTSensor>>($"api/device/get-sensor?page=0&pageSize={pageSize}", cancellationToken);
        return result;
    }
}