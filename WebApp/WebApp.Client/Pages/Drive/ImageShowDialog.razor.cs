using BusinessModels.System.FileSystem;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Pages.Drive;

public partial class ImageShowDialog : ComponentBase
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    [Parameter] public required FileInfoModel File { get; set; }


    private bool _open;
    private string ImageUrl { get; set; } = string.Empty;


    protected override void OnParametersSet()
    {
        ImageUrl = $"api/files/get-file?id={File.Id.ToString()}";
        base.OnParametersSet();
    }

    private void CloseDialog()
    {
        DialogInstance.Close();
    }

    private Task ChangeSideBar()
    {
        _open = !_open;
        return InvokeAsync(StateHasChanged);
    }
}