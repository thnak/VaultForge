using BusinessModels.Validator.Folder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace WebApp.Client.Pages.Drive.SharedDrive;

public partial class AddNewFolderDialog : ComponentBase, IDisposable
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    private MudForm Form { get; set; }
    private string Name { get; set; } = string.Empty;

    private FolderNameFluentValidator Validator { get; set; } = new();
    
    private void Cancel(MouseEventArgs obj)
    {
        DialogInstance.Cancel();
    }

    private async Task Submit(MouseEventArgs obj)
    {
        await Form.Validate();
        if (Form.IsValid)
        {
            DialogInstance.Close(Name);
        }
    }

    public void Dispose()
    {
        Form.Dispose();
    }
}