using System.Web;
using BusinessModels.System.InternetOfThings;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<List<IoTSensor>> GetIoTSensorsByDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<List<IoTSensor>>($"/api/device/get-sensor-by-device-id?deviceId={HttpUtility.UrlEncode(deviceId)}", cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            return result.Data;
        }

        return [];
    }
}