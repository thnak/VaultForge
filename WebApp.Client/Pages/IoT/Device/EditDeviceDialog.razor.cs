using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Pages.IoT.Device;

public partial class EditDeviceDialog : ComponentBase
{
    [CascadingParameter] private MudDialogInstance Dialog { get; set; } = default!;

    [Parameter] public IoTDevice? Device { get; set; }

    private IoTDevice DeviceToEdit { get; set; } = new IoTDevice();

    protected override void OnParametersSet()
    {
        DeviceToEdit = Device ?? DeviceToEdit;
        base.OnParametersSet();
    }
}