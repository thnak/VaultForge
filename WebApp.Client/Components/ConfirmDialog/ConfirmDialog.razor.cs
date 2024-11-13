using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Models;

namespace WebApp.Client.Components.ConfirmDialog;

public partial class ConfirmDialog : ComponentBase
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    [Parameter] public DialogConfirmDataModel DataModel { get; set; } = new();

    private void Cancel()
    {
        DialogInstance.Cancel();
    }

    private void Submit()
    {
        DialogInstance.Close();
    }
}