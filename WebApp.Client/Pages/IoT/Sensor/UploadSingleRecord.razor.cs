using BusinessModels.Resources;
using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.IoT.Sensor;

public partial class UploadSingleRecord(ILogger<UploadSingleRecord> logger) : ComponentBase, IDisposable
{
    private MudForm? _Form;
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;

    [Parameter] public required IoTSensor SensorId { get; set; }
    [Parameter] public required IoTDevice DeviceId { get; set; }
    private bool DisableSubmitBtn => File == null;

    private IBrowserFile? File {get; set;}
    
    private void UploadFiles(IBrowserFile? file)
    {
        File = file;
    }

    public void Dispose()
    {
        _Form?.Dispose();
    }

    private void Cancel()
    {
        DialogInstance.Cancel();
    }

    private async Task Submit()
    {
        await _Form!.Validate();
        if (_Form.IsValid)
        {
            MultipartFormDataContent form = new();
            await using var stream = File!.OpenReadStream(16 * 1024 * 1024);
            form.Add(new StreamContent(stream), "file", File.Name);
            form.Add(new StringContent(SensorId.SensorId), "sensorId");
            var postResult = await ApiService.PostAsync("api/iot/add-image", form);
            if(postResult.IsSuccessStatusCode)
                ToastService.ShowSuccess(AppLang.Success, TypeClassList.ToastDefaultSetting);
            else
            {
                ToastService.ShowError(AppLang.Create_failed, TypeClassList.ToastDefaultSetting);
                logger.LogError(postResult.Message);
            }
        }
    }
}