using BusinessModels.Secure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace CodeWithMe.Client.Pages.User;

public partial class LoginPage : ComponentBase
{
    [Parameter] [SupplyParameterFromQuery] public string ReturnUrl { get; set; } = string.Empty;
    [Parameter] [SupplyParameterFromQuery] public string? ErrorMessage { get; set; }

    private RequestLoginModel Model { get; } = new();
    private MudForm MudForm { get; set; } = default!;

    private bool IsValid { get; set; }

    private bool IsShow { get; set; }
    private string PasswordInputIcon { get; set; } = Icons.Material.Filled.VisibilityOff;
    private InputType PasswordInput { get; set; } = InputType.Password;

    public Dictionary<string, object> UserAttributes { get; set; } = new()
    {
        {
            "method", "post"
        },
        {
            "action", "api/User/Login"
        }
    };

    private string[] FormError { get; set; } = [];
    

    private void PasswordIconClick()
    {
        if (IsShow)
        {
            IsShow = false;
            PasswordInputIcon = Icons.Material.Filled.VisibilityOff;
            PasswordInput = InputType.Password;
        }
        else
        {
            IsShow = true;
            PasswordInputIcon = Icons.Material.Filled.Visibility;
            PasswordInput = InputType.Text;
        }
    }

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            IsValid = false;
            FormError = [ErrorMessage, ErrorMessage];
        }

        Model.ReturnUrl = ReturnUrl;
        InvokeAsync(StateHasChanged);
    }

    private void ValidChanged(bool obj)
    {
        IsValid = obj;
        if (!IsValid) ErrorMessage = string.Empty;
        StateHasChanged();
    }
}