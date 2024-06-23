using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;

namespace Web.Client.Pages.Account;

public partial class Login : ComponentBase
{
    [Parameter] public string ErrorMessage { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        ErrorMessage = ErrorMessage.DecodeBase64String();
        base.OnParametersSet();
    }
}