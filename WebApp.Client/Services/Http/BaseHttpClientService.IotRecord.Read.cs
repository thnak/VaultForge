using BusinessModels.General.Results;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<ResponseDataResult<SignalrResultValue<IoTRecord>>> GetIotRecordsAsync(string sensorId, int page, int pageSize, DateTime startTime, DateTime endTime)
    {
        MultipartFormDataContent form = new();
        form.Add(new StringContent(sensorId), "sensorId");
        form.Add(new StringContent(page.ToString()), "page");
        form.Add(new StringContent(pageSize.ToString()), "pageSize");
        form.Add(new StringContent(startTime.ToString("o")), "startTime");
        form.Add(new StringContent(endTime.ToString("o")), "endTime");
        var data = await PostAsync<SignalrResultValue<IoTRecord>>("/api/iot/get-record", form);
        return data;
    }

    public string GenerateUrlDownloadLink(string sensorId, int page, int pageSize, DateTime startTime, DateTime endTime)
    {
        var url = Navigation.GetUriWithQueryParameters(Navigation.BaseUri + "api/iot/get-excel-record", new Dictionary<string, object?>()
        {
            { "sensorId", sensorId },
            { "page", page },
            { "pageSize", pageSize },
            { "startTime", startTime.ToString("o") },
            { "endTime", endTime.ToString("o") }
        });
        return url;
    }
}