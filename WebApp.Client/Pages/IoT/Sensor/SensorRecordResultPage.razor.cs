using System.Globalization;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Models;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.IoT.Sensor;

public partial class SensorRecordResultPage : ComponentBase
{
    #region --- page models ---

    private class PageModel(IoTRecord device)
    {
        public IoTRecord Device { get; set; } = device;
        public ButtonAction OpenImageBtn { get; set; } = new();
        public ButtonAction DeleteBtn { get; set; } = new();
        public ButtonAction UpdateBtn { get; set; } = new();
    }

    private class FilterPageModel
    {
        public IEnumerable<IoTDevice> SelectedDevices { get; set; } = [];
        public IoTDevice? SelectedDevice { get; set; }
        public IEnumerable<IoTSensor> Sensors { get; set; } = [];
        public IoTSensor? SelectedSensor { get; set; }


        public DateRange DateRange { get; set; } = new() { Start = DateTime.Now, End = DateTime.Now };
    }

    #endregion

    private MudDataGrid<PageModel>? _dataGrid;
    private FilterPageModel _filterPage = new();

    private string DeviceSearchString { get; set; } = string.Empty;

    private List<IoTDevice> DevicesList { get; set; } = new List<IoTDevice>();
    private List<IoTSensor> SensorList { get; set; } = new List<IoTSensor>();
    private bool OpenFilterState { get; set; } = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetDevices();
            await GetSensors();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task GetDevices()
    {
        int pageSize = int.MaxValue;
        var result = await ApiService.GetAsync<SignalrResultValue<IoTDevice>>($"api/device/get-device?page=0&pageSize={pageSize}");
        if (!result.IsSuccessStatusCode)
        {
            ToastService.ShowError(result.Message, TypeClassList.ToastDefaultSetting);
        }

        DevicesList = [..result.Data?.Data ?? []];
        _filterPage.SelectedDevices = [..DevicesList];
    }

    private async Task GetSensors()
    {
        int pageSize = int.MaxValue;
        var result = await ApiService.GetAsync<SignalrResultValue<IoTSensor>>($"api/device/get-sensor?page=0&pageSize={pageSize}");
        if (!result.IsSuccessStatusCode)
        {
            ToastService.ShowError(result.Message, TypeClassList.ToastDefaultSetting);
        }

        SensorList = [..result.Data?.Data ?? []];
        _filterPage.Sensors = [..SensorList];
    }

    private async Task<IEnumerable<string>> SearchDevice(string arg1, CancellationToken arg2)
    {
        return [];
    }

    private async Task<GridData<PageModel>> ServerReload(GridState<PageModel> arg)
    {
        List<IoTRecord> records = new();
        long total = 0;
        foreach (var sensor in _filterPage.Sensors)
        {
            MultipartFormDataContent form = new();
            form.Add(new StringContent(sensor.SensorId), "sensorId");
            form.Add(new StringContent(arg.Page.ToString()), "page");
            form.Add(new StringContent(arg.PageSize.ToString()), "pageSize");
            form.Add(new StringContent(_filterPage.DateRange.Start.GetValueOrDefault(DateTime.Now).ToUnixDate().Round(0).ToString(CultureInfo.InvariantCulture)), "startDate");
            form.Add(new StringContent(_filterPage.DateRange.End.GetValueOrDefault(DateTime.Now).ToUnixDate().Round(0).ToString(CultureInfo.InvariantCulture)), "endDate");
            var data = await ApiService.PostAsync<SignalrResultValue<IoTRecord>>("/api/iot/get-record", form);
            if (data.IsSuccessStatusCode)
            {
                total = data.Data.Total;
                records.AddRange(data.Data.Data ?? []);
            }
            else
            {
                ToastService.ShowError(data.Message, TypeClassList.ToastDefaultSetting);
            }
        }

        return new GridData<PageModel>()
        {
            TotalItems = (int)total,
            Items = records.OrderByDescending(x => x.Timestamp).Select(x => new PageModel(x)).ToArray()
        };
    }

    private static async Task OpenImage(string code)
    {
        
    }
    
    private string ProcessStatusStyle(PageModel arg)
    {
        switch (arg.Device.Metadata.ProcessStatus)
        {
            case ProcessStatus.Requesting:
                return "background-color:red;color:white;";
            case ProcessStatus.Processing:
                return "background-color:blue;color:white;";
            case ProcessStatus.Completed:
                return "background-color:green;color:white;";
        }

        return "";
    }

    private Task OpenFilter()
    {
        OpenFilterState = !OpenFilterState;
        return InvokeAsync(StateHasChanged);
    }

    private Task CancelFilter()
    {
        OpenFilterState = false;
        return InvokeAsync(StateHasChanged);
    }

    private async Task SubmitFilter()
    {
        await CancelFilter();
        await _dataGrid!.ReloadServerData();
    }
}