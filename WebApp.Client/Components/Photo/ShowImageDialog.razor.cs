using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Components.Photo;

public partial class ShowImageDialog : ComponentBase
{
    [CascadingParameter] private  MudDialogInstance DialogInstance { get; set; } = null!;
    [Parameter] public string Uri { get; set; } = string.Empty;
    [Parameter] public string Caption { get; set; } = string.Empty;
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string Icon { get; set; } = string.Empty;

    private void CloseDialog()
    {
        DialogInstance.Close();
    }
}