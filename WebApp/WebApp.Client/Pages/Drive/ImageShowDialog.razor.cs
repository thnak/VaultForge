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
    public Dictionary<string, object?> ImageAttribute { get; set; } = [];


    protected override void OnParametersSet()
    {
        ImageUrl = $"api/files/get-file?id={File.Id.ToString()}";
        ImageAttribute.Add("alt", File.FileName);
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