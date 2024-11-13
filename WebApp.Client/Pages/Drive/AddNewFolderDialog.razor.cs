using BusinessModels.Resources;
using BusinessModels.Utils;
using BusinessModels.Validator;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace WebApp.Client.Pages.Drive;

public partial class AddNewFolderDialog : ComponentBase, IDisposable
{
    [CascadingParameter] private MudDialogInstance DialogInstance { get; set; } = default!;
    private MudForm? Form { get; set; }
    private string Name { get; set; } = string.Empty;

    private SimpleFluentValueValidator<string> Validator { get; } = new(x => x
        .NotEmpty().WithMessage(AppLang.ThisFieldIsRequired)
        .Length(1, 100)
        .Must(s => s.ValidateSystemPathName()).WithMessage(s =>
        {
            s.ValidateSystemPathName(out var c);
            return string.Format(AppLang.Folder_name_contains_invalid_character__x, c);
        }));

    public void Dispose()
    {
        if (Form != null) Form.Dispose();
    }

    private void Cancel(MouseEventArgs obj)
    {
        DialogInstance.Cancel();
    }

    private async Task Submit(MouseEventArgs obj)
    {
        if (Form != null)
        {
            await Form.Validate();
            if (Form.IsValid) DialogInstance.Close(Name);
        }
    }
}