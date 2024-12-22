using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.IoT.Device;

public partial class DeviceManagementPage : ComponentBase, IDisposable
{
    #region --- page models ---

    private class PageModel(IoTDevice device)
    {
        public IoTDevice Device { get; set; } = device;
        public ButtonAction DeleteBtn { get; set; } = new();
        public ButtonAction UpdateBtn { get; set; } = new();
    }

    #endregion

    private MudDataGrid<PageModel>? _dataGrid;

    private List<PageModel> Devices { get; set; } = new();
    private string DeviceSearchString { get; set; } = string.Empty;


    public void Dispose()
    {
        _dataGrid?.Dispose();
    }

    private async Task<GridData<PageModel>> ServerReload(GridState<PageModel> arg)
    {
        var result = await ApiService.GetAsync<SignalRResultValue<IoTDevice>>("api/device/get-device");
        return new GridData<PageModel>
        {
            Items = result.Data?.Data.Select(x => new PageModel(x)
            {
                DeleteBtn = new()
                {
                    Action = () => ConfirmDelete(x).ConfigureAwait(false)
                },
                UpdateBtn = new()
                {
                    Action = () => UpdateDevice(x).ConfigureAwait(false)
                }
            }) ?? [],
            TotalItems = (int)(result.Data?.Total ?? 0)
        };
    }

    private async Task ConfirmDelete(IoTDevice device)
    {
        var data = new DialogConfirmDataModel
        {
            Fragment = builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, $"delete device {device.DeviceName}");
                builder.CloseElement();
            },
            Icon = "fa-solid fa-triangle-exclamation",
            Color = Color.Error
        };
        var option = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };
        var parameter = new DialogParameters<ConfirmDialog>
        {
            { x => x.DataModel, data }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(AppLang.Warning, parameter, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            var response = await ApiService.DeleteAsync<string>($"/api/device/delete-device?deviceId={device.DeviceId}");
            if (response.IsSuccessStatusCode)
            {
                await _dataGrid!.ReloadServerData();
                ToastService.ShowSuccess(AppLang.Delete_successfully, TypeClassList.ToastDefaultSetting);
            }
            else
            {
                ToastService.ShowError(response.Message, TypeClassList.ToastDefaultSetting);
            }
        }
    }

    private async Task UpdateDevice(IoTDevice? device)
    {
        var option = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            BackgroundClass = "blur-3"
        };
        var param = new DialogParameters<EditDeviceDialog>
        {
            { x => x.Device, device }
        };
        var dialog = await DialogService.ShowAsync<EditDeviceDialog>("", param, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: bool status })
        {
            if (status) await _dataGrid!.ReloadServerData();
        }
    }

    private async Task<IEnumerable<string>> SearchDevice(string arg1, CancellationToken arg2)
    {
        var result = await ApiService.GetAsync<List<IoTDevice>>("api/device/search-device", arg2);
        return result.Data?.Select(x => x.DeviceId) ?? [];
    }

    private async Task OpenAddDialog()
    {
        await UpdateDevice(null);
    }
}