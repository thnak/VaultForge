using Microsoft.AspNetCore.Components;

namespace Web.Client.Pages.Account;

public partial class SignUpPage : ComponentBase
{
    [Parameter] public string ErrorMessage { get; set; } = string.Empty;
}