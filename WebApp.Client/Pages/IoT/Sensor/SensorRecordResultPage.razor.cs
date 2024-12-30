using BusinessModels.System.InternetOfThings;
using BusinessModels.System.InternetOfThings.type;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Models;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.IoT.Sensor;

public partial class SensorRecordResultPage : ComponentBase, IDisposable
{
    #region --- page models ---

    private class PageModel
    {
        public IoTRecord Device { get; set; }
        public ButtonAction OpenImageBtn { get; set; } = new();

        public PageModel(IoTRecord device)
        {
            device.CreateTime = device.CreateTime.ToLocalTime();
            device.Metadata.RecordedAt = device.Metadata.RecordedAt.ToLocalTime();
            Device = device;
        }
    }

    private class FilterPageModel
    {
        public IoTDevice? SelectedDevice { get; set; }
        public IEnumerable<IoTSensor> Sensors { get; set; } = [];
        public IoTSensor? SelectedSensor { get; set; }
        public DateRange DateRange { get; set; } = new() { Start = DateTime.Now, End = DateTime.Now };
    }

    #endregion

    private MudDataGrid<PageModel>? _dataGrid;
    private FilterPageModel _filterPage = new();

    private string DeviceSearchString { get; set; } = string.Empty;

    private List<IoTSensor> SensorList { get; set; } = new();
    private bool OpenFilterState { get; set; }
    private MudForm? FilterForm { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _dataGrid!.ReloadServerData().ConfigureAwait(false);
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    private async Task<IEnumerable<IoTDevice>> SearchDevice(string arg1, CancellationToken arg2)
    {
        return await ApiService.SearchDevicesAsync(arg1, arg2);
    }

    private async Task<GridData<PageModel>> ServerReload(GridState<PageModel> arg)
    {
        List<IoTRecord> records = new();
        long total = 0;
        foreach (var sensor in _filterPage.Sensors)
        {
            var data = await ApiService.GetIotRecordsAsync(sensor.SensorId, arg.Page, arg.PageSize,
                _filterPage.DateRange.Start.GetValueOrDefault(DateTime.Now).Date,
                _filterPage.DateRange.End.GetValueOrDefault(DateTime.Now).Date);
            if (data.IsSuccessStatusCode)
            {
                total = data.Data.Total;
                records.AddRange(data.Data.Data);
            }
            else
            {
                ToastService.ShowError(data.Message, TypeClassList.ToastDefaultSetting);
            }
        }

        return new GridData<PageModel>()
        {
            TotalItems = (int)total,
            Items = records.OrderByDescending(x => x.Metadata.RecordedAt).Select(x => new PageModel(x)
            {
                OpenImageBtn = new ButtonAction() { Action = () => OpenImage(x.Metadata.ImagePath).ConfigureAwait(false) }
            }).ToArray()
        };
    }

    private async Task OpenImage(string code)
    {
        var uri = Navigation.GetUriWithQueryParameters(Navigation.BaseUri + "api/files/get-file", new Dictionary<string, object?>()
        {
            { "id", code },
            { "type", "ThumbnailWebpFile" }
        });
        await JsRuntime.OpenToNewWindow(uri);
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
        await FilterForm!.Validate();
        if (FilterForm.IsValid)
        {
            await CancelFilter();
            await _dataGrid!.ReloadServerData();
        }
    }

    private async Task DownloadExcel()
    {
        foreach (var sensor in _filterPage.Sensors)
        {
            var uri = ApiService.GenerateUrlDownloadLink(sensor.SensorId, 0, Int32.MaxValue,
                _filterPage.DateRange.Start.GetValueOrDefault(DateTime.Now).Date,
                _filterPage.DateRange.End.GetValueOrDefault(DateTime.Now).Date);
            await JsRuntime.Download(uri);
        }
    }

    private async Task SelectedDeviceChanged(IoTDevice? arg)
    {
        _filterPage.SelectedDevice = arg;
        var result = await ApiService.GetIotSensorFromDeviceAsync(_filterPage.SelectedDevice?.DeviceId ?? string.Empty);
        SensorList = [..result];
        _filterPage.Sensors = [..SensorList];
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        _dataGrid?.Dispose();
        FilterForm?.Dispose();
    }
}