using System.Net.Mime;
using System.Text;
using System.Web;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.IoT.Device;

public partial class EditSensorDialog(ILogger<EditSensorDialog> logger) : ComponentBase, IDisposable
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;

    [Parameter] public IoTSensor? Sensor { get; set; }

    [Parameter] public required string DeviceId { get; set; }

    private bool IsEditing => Sensor != null;
    private string DialogIcon => IsEditing ? Icons.Material.Filled.Edit : Icons.Material.Filled.Add;
    private string ConfirmButtonText => Sensor == null ? AppLang.Create_new : AppLang.Update;

    private IoTSensor SensorToEdit { get; set; } = new();
    private bool Processing { get; set; }


    private MudForm? Form { get; set; }
    public DateTime? CalibrationTime { get; set; } = DateTime.Now;
    private string[] ErrorMess { get; set; } = [];

    private readonly IoTSensorFluentValidator _orderValidator = new();


    protected override Task OnParametersSetAsync()
    {
        SensorToEdit = Sensor ?? new IoTSensor();
        SensorToEdit.DeviceId = string.IsNullOrEmpty(SensorToEdit.DeviceId) ? DeviceId : SensorToEdit.DeviceId;
        CalibrationTime = SensorToEdit.CalibrationTime ?? CalibrationTime;
        _orderValidator.ValidateForCreate = Sensor == null;
        _orderValidator.CheckDeviceExists = CheckDeviceExists;
        _orderValidator.CheckSensorExists = CheckSensorExists;
        return base.OnParametersSetAsync();
    }

    private async Task<bool> CheckSensorExists(string arg1, CancellationToken arg2)
    {
        arg1 = HttpUtility.UrlEncode(arg1);
        var result = await ApiService.GetAsync<IoTDevice>($"/api/device/get-sensor-by-id?id={arg1}", arg2);
        return result.Data == null;
    }

    private async Task<bool> CheckDeviceExists(string arg1, CancellationToken arg2)
    {
        arg1 = HttpUtility.UrlEncode(arg1);
        var result = await ApiService.GetAsync<IoTDevice>($"/api/device/get-device-by-id?deviceId={arg1}", arg2);
        return result.Data != null;
    }

    private void CancelForm()
    {
        DialogInstance.Cancel();
    }

    private async Task SubmitForm()
    {
        Processing = true;
        try
        {
            await Form!.Validate();
            if (Form.IsValid)
            {
                if (Sensor == null)
                {
                    var textPlant = new StringContent(SensorToEdit.ToJson(), Encoding.UTF8, MediaTypeNames.Application.Json);
                    var result = await ApiService.PostAsync<string>($"/api/device/add-new-sensor/{Uri.EscapeDataString(DeviceId)}", textPlant);
                    if (result.IsSuccessStatusCode)
                    {
                        ToastService.ShowSuccess(result.Message, TypeClassList.ToastDefaultSetting);
                        DialogInstance.Close(DialogResult.Ok(true));
                    }
                    else
                    {
                        ToastService.ShowError(result.Message, TypeClassList.ToastDefaultSetting);
                        ErrorMess = [result.Message];
                    }
                }
                else
                {
                    var field2Update = new FieldUpdate<IoTSensor>()
                    {
                        { x => x.SensorName, SensorToEdit.SensorName },
                        { x => x.Status, SensorToEdit.Status },
                        { x => x.IoTSensorType, SensorToEdit.IoTSensorType },
                        { x => x.Accuracy, SensorToEdit.Accuracy },
                        { x => x.Rotate, SensorToEdit.Rotate },
                        { x => x.UnitOfMeasurement, SensorToEdit.UnitOfMeasurement },
                    };
                    using var content = new MultipartFormDataContent();

                    content.Add(new StringContent(SensorToEdit.SensorId), "sensorId");
                    content.Add(new StringContent(field2Update.GetJson()), "json");
                    var result = await ApiService.PostAsync<string>("/api/device/update-sensor", content);
                    if (result.IsSuccessStatusCode)
                    {
                        ToastService.ShowSuccess(result.Message, TypeClassList.ToastDefaultSetting);
                        DialogInstance.Close(DialogResult.Ok(true));
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

    public void Dispose()
    {
        Form?.Dispose();
    }

    private Task CalibrationTimeChanged(DateTime? arg)
    {
        CalibrationTime = arg;
        SensorToEdit.CalibrationTime = arg;
        return Task.CompletedTask;
    }
}