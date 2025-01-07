using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;
using WebApp.Client.Utils;
using DataGridExtensions = WebApp.Client.Utils.RazorExtensions.DataGridExtensions;

namespace WebApp.Client.Pages.IoT.Device;

public partial class DeviceManagementPage(ILogger<DeviceManagementPage> logger) : ComponentBase, IDisposable
{
    #region --- page models ---

    private class PageModel(IoTDevice device)
    {
        public IoTDevice Device { get; } = device;
        public RenderFragment? ActionContent { get; set; }
    }

    #endregion

    private MudDataGrid<PageModel>? _dataGrid;
    private string DeviceSearchString { get; set; } = string.Empty;
    private readonly DataGridExtensions.DataGridExtensionsBuilder _builderHelper = new();
    private bool AllowRendering { get; set; } = false;

    protected override bool ShouldRender() => AllowRendering;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ReloadPage();
        }
    }

    public void Dispose()
    {
        _dataGrid?.Dispose();
    }

    private async Task<GridData<PageModel>> ServerReload(GridState<PageModel> arg)
    {
        AllowRendering = true;
        try
        {
            var result = await ApiService.GetAllDevicesAsync(arg.Page, arg.PageSize);
            return new GridData<PageModel>
            {
                Items = result.Data?.Data.Select(x =>
                {
                    var model = new PageModel(x);
                    var deleteBtn = new ButtonAction
                    {
                        Action = () => ConfirmDelete(x).ConfigureAwait(false),
                        Title = AppLang.Delete,
                        Icon = Icons.Material.Filled.Delete,
                        ButtonColor = Color.Error,
                        ButtonSize = Size.Small
                    };
                    var updateBtn = new ButtonAction
                    {
                        Action = () => UpdateDevice(x).ConfigureAwait(false),
                        Title = AppLang.Delete,
                        Icon = Icons.Material.Filled.Edit,
                        ButtonColor = Color.Default,
                        ButtonSize = Size.Small
                    };
                    model.ActionContent = _builderHelper.GenerateTableAction([updateBtn, deleteBtn]);
                    return model;
                }) ?? [],
                TotalItems = (int)(result.Data?.Total ?? 0)
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new GridData<PageModel>();
        }
        finally
        {
            AllowRendering = false;
        }
    }

    private async Task ConfirmDelete(IoTDevice device)
    {
        AllowRendering = true;
        try
        {
            var data = new DialogConfirmDataModel
            {
                Fragment = builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, $"{LangDict[AppLang.Delete_device]} {device.DeviceName}");
                    builder.CloseElement();
                },
                TitleIcon = "fa-solid fa-triangle-exclamation",
                Color = Color.Error
            };

            if (await DialogService.OpenConfirmDialogAsync(LangDict[AppLang.Warning], data))
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
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
        finally
        {
            AllowRendering = false;
        }
    }

    private async Task UpdateDevice(IoTDevice? device)
    {
        AllowRendering = true;
        try
        {
            var param = new DialogParameters<EditDeviceDialog>
            {
                { x => x.Device, device }
            };
            var dialog = await DialogService.ShowAsync<EditDeviceDialog>(device == null ? AppLang.Add_new_device : AppLang.Edit, param, DialogServiceExtensions.ConfirmDialogOptionsLarge);
            var dialogResult = await dialog.Result;
            if (dialogResult is { Canceled: false, Data: bool status })
            {
                if (status) await _dataGrid!.ReloadServerData();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
        finally
        {
            AllowRendering = false;
        }
    }

    private async Task<IEnumerable<string>> SearchDevice(string arg1, CancellationToken arg2)
    {
        AllowRendering = true;
        try
        {
            var result = await ApiService.SearchDevicesAsync(arg1, arg2);
            return result.Select(x => x.DeviceName);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return [];
        }
        finally
        {
            AllowRendering = false;
        }
    }

    private async Task OpenAddDialog()
    {
        await UpdateDevice(null);
    }

    private Task ReloadPage()
    {
        return _dataGrid!.ReloadServerData();
    }
}