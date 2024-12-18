using BusinessModels.Resources;
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

    private Task<GridData<PageModel>> ServerReload(GridState<PageModel> arg)
    {
        var sortDefinition = arg.SortDefinitions.FirstOrDefault();
        // if (sortDefinition != null)
        // {
        //     switch (sortDefinition.SortBy)
        //     {
        //         case nameof(PageModel.Device)
        //     }
        // }
        

        throw new NotImplementedException();
    }


    private Task<IEnumerable<string>> SearchDevice(string arg1, CancellationToken arg2)
    {
        throw new NotImplementedException();
    }

    private Task OpenAddDialog()
    {
        var options = new DialogOptions { BackgroundClass = "blur-3" };
        return DialogService.ShowAsync<EditDeviceDialog>(AppLang.Add_new_device, options);
    }
}