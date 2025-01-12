using BusinessModels.General.EnumModel;
using BusinessModels.System.FileSystem;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Pages.Drive.View.Dialog;

public partial class ImageShowDialog : ComponentBase
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    [Parameter] public required FileInfoModel File { get; set; }

    private bool _open;
    private string ImageUrl { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        if (File.ContentType.IsImageFile())
            ImageUrl = $"api/files/get-file?id={File.AliasCode}&type={FileClassify.ThumbnailWebpFile}";
        else if (File.ContentType.IsVideoFile())
            ImageUrl = $"api/files/stream-raid?path={File.AliasCode}";

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