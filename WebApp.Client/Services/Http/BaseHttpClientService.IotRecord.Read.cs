using System.Globalization;
using BusinessModels.General.Results;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<ResponseDataResult<SignalrResultValue<IoTRecord>>> GetIotRecordsAsync(string sensorId, int page, int pageSize, double unixStartDate, double unixEndDate)
    {
        MultipartFormDataContent form = new();
        form.Add(new StringContent(sensorId), "sensorId");
        form.Add(new StringContent(page.ToString()), "page");
        form.Add(new StringContent(pageSize.ToString()), "pageSize");
        form.Add(new StringContent(unixStartDate.ToString(CultureInfo.InvariantCulture)), "startDate");
        form.Add(new StringContent(unixEndDate.ToString(CultureInfo.InvariantCulture)), "endDate");
        var data = await PostAsync<SignalrResultValue<IoTRecord>>("/api/iot/get-record", form);
        return data;
    }
}