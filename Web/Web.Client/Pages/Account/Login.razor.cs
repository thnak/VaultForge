using BusinessModels.Secure;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Web.Client.Pages.Account;

public partial class Login : ComponentBase, IDisposable
{
    [Parameter] public string ErrorMessage { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        ErrorMessage = ErrorMessage.DecodeBase64String();
        base.OnParametersSet();
    }

    private MudForm? form { get; set; }
    private RequestLoginModel CurrentRequestModel { get; set; } = new();
    public void Dispose()
    {
        form?.Dispose();
    }
}