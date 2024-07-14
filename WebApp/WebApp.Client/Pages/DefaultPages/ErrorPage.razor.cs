using System.Diagnostics;
using BusinessModels.System;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Pages.DefaultPages;

public partial class ErrorPage : ComponentBase
{
    // [CascadingParameter] private HttpContext? HttpContext { get; set; }
    [Parameter] public string ErrorMessage { get; set; } = string.Empty;
    [Parameter] public string ReturnUrl { get; set; } = "/";
    [Parameter] public Exception? Exception { get; set; } = null;

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    private string HandingMessage { get; set; } = string.Empty;
    private ErrorRecordModel RecordModel { get; set; } = new();
    private bool SuccessHanding { get; set; }
    protected override void OnInitialized()
    {
        RequestId = Activity.Current?.Id ?? string.Empty;
    }

    protected override void OnParametersSet()
    {
        RecordModel = ErrorMessage.DecodeBase64String<ErrorRecordModel>() ?? RecordModel;
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await UpdateHandingMessage("Collecting information...");
            if (Exception != null)
            {
                ErrorMessage = Exception.Message;
                await UpdateHandingMessage("Processing...");
                await UpdateHandingMessage("Collecting information...");
            }
            else
            {
                await UpdateHandingMessage(RecordModel.RequestId);
                await UpdateHandingMessage(RecordModel.Href);
                await UpdateHandingMessage(RecordModel.Src);
                await UpdateHandingMessage(RecordModel.Message);
            }
            await UpdateHandingMessage("Processing...");
            await UpdateHandingMessage("Now you can go back");

            SuccessHanding = true;
            await Task.Delay(1);
            await InvokeAsync(StateHasChanged);
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task UpdateHandingMessage(string message)
    {
        HandingMessage = message;
        await Task.Delay(1000);
        await InvokeAsync(StateHasChanged);
    }
}