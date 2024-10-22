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
        if(File.ExtendResource.Any(z => z.Classify == FileClassify.M3U8File))
            ImageUrl = $"api/files/get-file?id={File.ExtendResource.FirstOrDefault(z=>z.Classify == FileClassify.M3U8File)}";
        else
        {
            if (File.ContentType.IsImageFile())
                ImageUrl = $"api/files/get-file?id={File.Id.ToString()}";
            else if (File.ContentType.IsVideoFile())
                ImageUrl = $"api/files/stream-raid?path={File.Id.ToString()}";
        }
        
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