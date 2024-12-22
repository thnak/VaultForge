using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Pages.IoT.Device;

public partial class DeviceManagementPage : ComponentBase, IDisposable
{
    #region --- page models ---

    private class PageModel(IoTDevice device)
    {
        public IoTDevice Device { get; set; } = device;
    }

    #endregion

    MudDataGrid<PageModel>? dataGrid;

    private List<PageModel> Devices { get; set; } = new();
    private string DeviceSearchString { get; set; } = string.Empty;


    public void Dispose()
    {
        dataGrid?.Dispose();
    }

    private async Task<GridData<PageModel>> ServerReload(GridState<PageModel> arg)
    {
        var result = await ApiService.GetAsync<SignalRResultValue<IoTDevice>>("api/device/get-device");
        return new GridData<PageModel>
        {
            Items = result.Data?.Data.Select(x => new PageModel(x)) ?? [],
            TotalItems = (int)(result.Data?.Total ?? 0)
        };
    }


    private async Task<IEnumerable<string>> SearchDevice(string arg1, CancellationToken arg2)
    {
        var result = await ApiService.GetAsync<List<IoTDevice>>("api/device/search-device", arg2);
        return result.Data?.Select(x => x.DeviceId) ?? [];
    }

    private Task OpenAddDialog()
    {
        var options = new DialogOptions { BackgroundClass = "blur-3" };
        return DialogService.ShowAsync<EditDeviceDialog>(AppLang.Add_new_device, options);
    }
}