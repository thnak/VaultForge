using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Models;

namespace WebApp.Client.Pages.IoT.DeviceGroup;

public partial class DeviceGroupManagementPage : ComponentBase
{
    #region --- page models ---

    private class PageModel(IoTDeviceGroup device)
    {
        public IoTDeviceGroup Device { get; set; } = device;
        public ButtonAction DeleteBtn { get; set; } = new();
        public ButtonAction UpdateBtn { get; set; } = new();
    }

    #endregion
    private IEnumerable<PageModel> Items { get; set; } = [];

}