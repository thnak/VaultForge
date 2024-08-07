using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WebApp.Client.Components.ConfirmDialog;

public partial class ConfirmWithFieldDialog : ComponentBase, IDisposable
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    [Parameter] public string FieldName { get; set; } = string.Empty;
    [Parameter] public string OldValueField { get; set; } = string.Empty;

    private string NewValueField { get; set; } = string.Empty;

    private MudForm? Form { get; set; }

    public void Dispose()
    {
        Form?.Dispose();
    }

    protected override void OnParametersSet()
    {
        NewValueField = OldValueField;
        base.OnParametersSet();
    }

    private async Task Submit()
    {
        if (Form != null)
        {
            await Form.Validate();
            if (Form.IsValid) DialogInstance.Close(DialogResult.Ok(NewValueField));
        }
    }

    private void Cancel()
    {
        DialogInstance.Cancel();
    }
}