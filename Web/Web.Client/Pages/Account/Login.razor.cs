using BusinessModels.Secure;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Web.Client.Pages.Account;

public partial class Login : ComponentBase, IDisposable
{
    [Parameter] [SupplyParameterFromQuery] public string ErrorMessage { get; set; } = string.Empty;
    [Parameter] [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    protected override void OnParametersSet()
    {
        ErrorMessage = ErrorMessage.DecodeBase64String();
        ReturnUrl = ReturnUrl?.DecodeBase64String();
        base.OnParametersSet();
    }

    private MudForm? form { get; set; }
    private bool IsValidForm { get; set; }

    private RequestLoginModel CurrentRequestModel { get; set; } = new();
    private string CurrentErrorMessage { get; set; } = string.Empty;
    private string PasswordIcon { get; set; } = "fa-solid fa-lock";

    private string[] FormError { get; set; } = [];
    private Dictionary<string, object> UserAttributes { get; set; } = new()
    {
        {
            "method", "post"
        },
        {
            "action", "api/Account/login"
        }
    };
    private InputType PasswordInput { get; set; }

    protected override async Task OnAfterRenderAsync(bool first)
    {
        CurrentRequestModel.ReturnUrl = ReturnUrl;
        if (CurrentErrorMessage != FormError.FirstOrDefault())
        {
            CurrentErrorMessage = ErrorMessage;
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                if (form != null)
                {
                    form.ResetValidation();
                    FormError = [ErrorMessage];
                    IsValidForm = false;
                    // await form.Validate();
                }
            }

        }

        await base.OnAfterRenderAsync(first);
    }

    private void PasswordShowEvent(MouseEventArgs obj)
    {
        PasswordIcon = PasswordIcon == "fa-solid fa-lock" ? "fa-solid fa-lock-open" : "fa-solid fa-lock";
        PasswordInput = PasswordIcon == "fa-solid fa-lock" ? InputType.Password : InputType.Text;
    }

    private void UsernameClickEvent()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        form?.Dispose();
    }
}