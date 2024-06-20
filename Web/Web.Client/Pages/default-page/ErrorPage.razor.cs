using System.Diagnostics;
using Microsoft.AspNetCore.Components;

namespace Web.Client.Pages.default_page;

public partial class ErrorPage : ComponentBase
{
    // [CascadingParameter] private HttpContext? HttpContext { get; set; }
    [Parameter] public string ErrorMessage { get; set; } = string.Empty;
    [Parameter] public string ReturnUrl { get; set; } = "/";
    [Parameter] public Exception? Exception { get; set; } = null;

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    private string HandingMessage { get; set; } = string.Empty;
        
    private bool SuccessHanding { get; set; }
    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? string.Empty;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (Exception != null)
            {
                ErrorMessage = Exception.Message;
                await UpdataHandingMessage("Processing...");
                await UpdataHandingMessage("Collecting information...");
                await UpdataHandingMessage("Processing...");
                await UpdataHandingMessage("Now you can go back");
                SuccessHanding = true;
                await Task.Delay(1);
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                await UpdataHandingMessage(ErrorMessage);
                await UpdataHandingMessage("Processing...");
                await UpdataHandingMessage("Processing...");
                SuccessHanding = true;
                await Task.Delay(1);
                await InvokeAsync(StateHasChanged);
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task UpdataHandingMessage(string message)
    {
        HandingMessage = message;
        await Task.Delay(1000);
        await InvokeAsync(StateHasChanged);
    }
}