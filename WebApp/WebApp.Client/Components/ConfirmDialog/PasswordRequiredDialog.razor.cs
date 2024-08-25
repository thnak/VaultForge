using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Components.ConfirmDialog;

public partial class PasswordRequiredDialog : ComponentBase
{
    [CascadingParameter] private MudDialogInstance Dialog { get; set; } = default!;
    private MudForm? Form { get; set; }
    private string Password { get; set; } = string.Empty;

    private void Cancel()
    {
        Dialog.Cancel();
    }

    private async Task Ok()
    {
        if (Form != default)
        {
            await Form.Validate();
            if (Form.IsValid)
            {
                Dialog.Close(Password);
            }
        }
    }
}