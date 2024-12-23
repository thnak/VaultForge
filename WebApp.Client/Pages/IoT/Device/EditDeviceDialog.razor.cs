using System.Net.Mime;
using System.Text;
using System.Web;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Models;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.IoT.Device;

public partial class EditDeviceDialog(ILogger<EditDeviceDialog> logger) : ComponentBase, IDisposable
{
    [CascadingParameter] private MudDialogInstance Dialog { get; set; } = default!;

    [Parameter] public IoTDevice? Device { get; set; }


    #region -- models --

    private class SensorPageM(IoTSensor sensor)
    {
        public IoTSensor IoTSensor { get; set; } = sensor;
        public ButtonAction EditButtonAction { get; set; } = new();
        public ButtonAction DeleteButtonAction { get; set; } = new();
    }

    #endregion


    private bool IsEditing => Device != null;
    private bool IsAddingDevice => Device == null;
    private string DialogIcon => IsEditing ? Icons.Material.Filled.Edit : Icons.Material.Filled.Add;
    private string ConfirmButtonText => Device == null ? AppLang.Create_new : AppLang.Update;


    private IoTDevice DeviceToEdit { get; set; } = new IoTDevice();
    private readonly IoTDeviceFluentValidator _orderValidator = new();

    private DateTime InstallationDate { get; set; } = DateTime.Today;
    private int ActivateIndex { get; set; }
    private bool Processing { get; set; }
    private MudForm? _form;
    private List<SensorPageM> Sensors { get; set; } = [];


    protected override void OnParametersSet()
    {
        DeviceToEdit = Device ?? DeviceToEdit;
        if (IsEditing)
        {
            DeviceToEdit.InstallationDate = DateOnly.FromDateTime(DateTime.Now);
        }

        InstallationDate = DeviceToEdit.InstallationDate.ToDateTime(TimeOnly.MinValue);
        _orderValidator.ValidateForCreate = Device is null;
        _orderValidator.CheckDeviceExists = CheckDeviceExists;
        base.OnParametersSet();
    }

    protected override Task OnParametersSetAsync()
    {
        return GetSensors();
    }

    private async Task<bool> CheckDeviceExists(string arg1, CancellationToken arg2)
    {
        arg1 = HttpUtility.UrlEncode(arg1);
        var result = await ApiService.GetAsync<IoTDevice>($"/api/device/get-device-by-id?deviceId={arg1}", arg2);
        return result.Data != null;
    }

    private async Task GetSensors()
    {
        var fetch = await ApiService.GetAsync<List<IoTSensor>>($"/api/device/get-sensor-by-device-id?deviceId={HttpUtility.UrlEncode(DeviceToEdit.DeviceId)}");
        Sensors = (fetch.Data ?? []).Select(x => new SensorPageM(x)
        {
            EditButtonAction = new() { Action = () => OpenEditSensorDialog(x).ConfigureAwait(false) },
            DeleteButtonAction = new() { Disabled = true }
        }).ToList();
    }

    private Task InstallationDateChanged(DateTime? arg)
    {
        InstallationDate = arg ?? DateTime.Today;
        DeviceToEdit.InstallationDate = DateOnly.FromDateTime(InstallationDate);
        return InvokeAsync(StateHasChanged);
    }

    private void CancelForm()
    {
        Dialog.Cancel();
    }

    private async Task SubmitForm()
    {
        Processing = true;
        try
        {
            await _form!.Validate();
            if (_form.IsValid)
            {
                if (Device == null)
                {
                    RequestToCreate requestToCreate = new RequestToCreate()
                    {
                        Device = DeviceToEdit,
                        Sensors = Sensors.Select(x => x.IoTSensor).ToList()
                    };
                    var textPlant = new StringContent(StringExtension.ToJson(requestToCreate), Encoding.UTF8, MediaTypeNames.Application.Json);
                    var result = await ApiService.PostAsync<string>("/api/device/add-new-device", textPlant);
                    if (result.IsSuccessStatusCode)
                    {
                        ToastService.ShowSuccess(result.Message, TypeClassList.ToastDefaultSetting);
                        Dialog.Close();
                    }
                    else
                    {
                        ToastService.ShowError(result.Message, TypeClassList.ToastDefaultSetting);
                    }
                }
                else
                {
                    var field2Update = new FieldUpdate<IoTDevice>()
                    {
                        { x => x.DeviceName, DeviceToEdit.DeviceName },
                        { x => x.Location, DeviceToEdit.Location },
                        { x => x.InstallationDate, DeviceToEdit.InstallationDate },
                        { x => x.MacAddress, DeviceToEdit.MacAddress },
                        { x => x.IpAddress, DeviceToEdit.IpAddress },
                        { x => x.Manufacturer, DeviceToEdit.Manufacturer },
                        { x => x.DeviceGroupId, DeviceToEdit.DeviceGroupId },
                        { x => x.Status, DeviceToEdit.Status },
                        { x => x.IoTDeviceType, DeviceToEdit.IoTDeviceType }
                    };
                    using var content = new MultipartFormDataContent();

                    content.Add(new StringContent(DeviceToEdit.DeviceId), "deviceId");
                    content.Add(new StringContent(field2Update.GetJson()), "json");
                    var result = await ApiService.PostAsync<string>("/api/device/update-device", content);
                    if (result.IsSuccessStatusCode)
                    {
                        ToastService.ShowSuccess(result.Message, TypeClassList.ToastDefaultSetting);
                        Dialog.Close(DialogResult.Ok(true));
                    }
                    else
                    {
                        ToastService.ShowError(result.Message, TypeClassList.ToastDefaultSetting);
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
        finally
        {
            Processing = false;
        }
    }

    private async Task OpenAddDialog()
    {
        await OpenEditSensorDialog(null);
    }

    private async Task OpenEditSensorDialog(IoTSensor? sensor)
    {
        var option = new DialogOptions()
        {
            BackgroundClass = "blur-3"
        };
        var param = new DialogParameters<EditSensorDialog>()
        {
            { x => x.Sensor, sensor },
            { x => x.DeviceId, DeviceToEdit.DeviceId }
        };
        var dialog = await DialogService.ShowAsync<EditSensorDialog>("Add new sensor", param, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false, Data: bool status })
        {
            if (status)
            {
                await GetSensors();
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    public void Dispose()
    {
        _form?.Dispose();
    }
}