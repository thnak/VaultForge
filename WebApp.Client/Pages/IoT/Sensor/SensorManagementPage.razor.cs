using BusinessModels.System.InternetOfThings;
using Microsoft.AspNetCore.Components;
using WebApp.Client.Models;

namespace WebApp.Client.Pages.IoT.Sensor;

public partial class SensorManagementPage : ComponentBase
{
    #region --- page models ---

    private class PageModel(IoTSensor device)
    {
        public IoTSensor Device { get; set; } = device;
        public ButtonAction DeleteBtn { get; set; } = new();
        public ButtonAction UpdateBtn { get; set; } = new();
    }

    #endregion

    private IEnumerable<PageModel> Items { get; set; } = [];
}