using BusinessModels.Secure;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using Web.Client.Services;

namespace Web.Client.Pages.Account;

public partial class Login(HttpClient httpClient, CustomAntiforgeryStateProvider antiforgeryStateProvider) : ComponentBase, IDisposable
{
    [Parameter] [SupplyParameterFromQuery] public string ErrorMessage { get; set; } = string.Empty;
    [Parameter] [SupplyParameterFromQuery] public string? ReturnUrl { get; set; }

    private MudForm? FormUser { get; set; }
    private MudForm? PasswordForm { get; set; }
    private RequestLoginModel CurrentRequestModel { get; } = new();
    private string CurrentErrorMessage { get; set; } = string.Empty;
    private string PasswordIcon { get; set; } = "fa-solid fa-lock";

    private string[] FormError { get; set; } = [];
    private InputType PasswordInput { get; set; } = InputType.Password;

    private int CurrentIndex { get; set; }
    private bool Loading { get; set; }
    private MudCarousel<object>? CarouselLogin { get; set; }
    private string? UserErrorText { get; set; }
    private string? PasswordErrorText { get; set; }
    public bool IsValid { get; set; }

    public void Dispose()
    {
        FormUser?.Dispose();
        PasswordForm?.Dispose();
        CarouselLogin?.DisposeAsync();
    }

    protected override void OnParametersSet()
    {
        ErrorMessage = ErrorMessage.DecodeBase64String();
        ReturnUrl = ReturnUrl?.DecodeBase64String();
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool first)
    {
        CurrentRequestModel.ReturnUrl = ReturnUrl;
        if (CurrentErrorMessage != FormError.FirstOrDefault())
        {
            CurrentErrorMessage = ErrorMessage;
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                if (FormUser != null)
                {
                    FormUser.ResetValidation();
                    FormError = [ErrorMessage];
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

    private async Task UsernameProcess(MouseEventArgs obj)
    {
        if (FormUser != null)
        {
            await FormUser.Validate();
            if (FormUser.IsValid)
            {
                Loading = true;
                await Task.Delay(1);
                await InvokeAsync(StateHasChanged);

                var antiforgery = await antiforgeryStateProvider.GetAntiforgeryTokenAsync();
                Console.WriteLine(antiforgery);
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(CurrentRequestModel.UserName), "userName");
                if (antiforgery != null)
                    content.Add(new StringContent(antiforgery.Value), antiforgery.FormFieldName);

                var response = await httpClient.PostAsync("/api/Account/validate-user", content);
                if (response.IsSuccessStatusCode)
                {
                    CurrentIndex++;
                }
                else
                {
                    UserErrorText = await response.Content.ReadAsStringAsync();
                    IsValid = false;
                    FormError = [UserErrorText];
                }

                Loading = false;
                await Task.Delay(1);
                await InvokeAsync(StateHasChanged);
            }
        }

    }
    private async Task PasswordProcess(MouseEventArgs obj)
    {
        if (PasswordForm != null)
        {
            await PasswordForm.Validate();
            if (PasswordForm.IsValid)
            {
                Loading = true;
                await Task.Delay(1);
                await InvokeAsync(StateHasChanged);
                var antiforgery = antiforgeryStateProvider.GetAntiforgeryToken();

                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(CurrentRequestModel.Password), "password");
                content.Add(new StringContent(CurrentRequestModel.UserName), "userName");
                if (antiforgery != null)
                    content.Add(new StringContent(antiforgery.Value), antiforgery.FormFieldName);


                var response = await httpClient.PostAsync("/api/Account/validate-password", content);
                if (response.IsSuccessStatusCode)
                {
                    CurrentIndex++;
                    Loading = false;
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(2000);

                    await JsRuntime.InvokeVoidAsync("ForceLogin");

                    // using var loginForm = new MultipartFormDataContent();
                    // loginForm.Add(new StringContent(CurrentRequestModel.Password), nameof(RequestLoginModel.Password));
                    // loginForm.Add(new StringContent(CurrentRequestModel.UserName), nameof(RequestLoginModel.UserName));
                    // loginForm.Add(new StringContent(CurrentRequestModel.ReturnUrl ?? string.Empty), nameof(RequestLoginModel.ReturnUrl));
                    // if (antiforgery != null)
                    //     loginForm.Add(new StringContent(antiforgery.Value), antiforgery.FormFieldName);
                    //
                    // response = await httpClient.PostAsync("/api/Account/login", loginForm);
                    // if (response.IsSuccessStatusCode)
                    // {
                    // }
                }
                else
                {
                    PasswordErrorText = await response.Content.ReadAsStringAsync();
                    IsValid = false;
                    FormError = [PasswordErrorText];
                }

                Loading = false;
                await Task.Delay(1);
                await InvokeAsync(StateHasChanged);

            }
        }

    }
    private void UsernameClickEvent()
    {
        InvokeAsync(StateHasChanged);
    }
}