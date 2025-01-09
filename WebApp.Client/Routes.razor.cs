using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using WebApp.Client.Models;
using WebApp.Client.Utils;

namespace WebApp.Client;

public partial class Routes(ILogger<Routes> logger) : ComponentBase, IDisposable
{
    private Guid ResizeId { get; set; }
    private string PreviousPathGroup  { get; set; } = string.Empty;
    public void Dispose()
    {
        CustomStateContainer.OnChangedAsync -= OnChangedAsync;
        BrowserViewportService.UnsubscribeAsync(ResizeId).ConfigureAwait(false);
        EventListener.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("CloseProgressBar").ConfigureAwait(false);
            await JsRuntime.InvokeVoidAsync("InitAppEventListener").ConfigureAwait(false);

            ResizeId = Guid.NewGuid();
            await BrowserViewportService.SubscribeAsync(ResizeId, ResizeAction, new ResizeOptions());

            CustomStateContainer.OnChangedAsync += OnChangedAsync;
            EventListener.ContextMenuClickedAsync += ContextMenuClicked;
            EventListener.PageShowEvent += PageShow;
            EventListener.PageHideEvent += PageHide;
            EventListener.Online += Online;
            EventListener.Offline += Offline;
            EventListener.InstalledEventAsync += InstalledWpa;
            EventListener.ScrollToReloadEventAsync += ScrollToReloadEventAsync;
            await ApiService.RequestCulture().ConfigureAwait(false);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private void ResizeAction(BrowserViewportEventArgs obj)
    {
        logger.LogInformation($"Height: {obj.BrowserWindowSize.Height}\nWidth: {obj.BrowserWindowSize.Width}");
        InvokeAsync(StateHasChanged);
    }

    private async Task<bool> ScrollToReloadEventAsync()
    {
        var dataModel = new DialogConfirmDataModel()
        {
            TitleIcon = "fa-solid fa-rotate-right",
            Color = Color.Secondary,
            Fragment = builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(0, AppLang.Do_you_really_want_to_reload_the_page_);
                builder.CloseElement();
            }
        };
      
        if (await DialogService.OpenConfirmDialogAsync(AppLang.Reload, dataModel))
        {
            return true;
        }

        return false;
    }

    private Task InstalledWpa()
    {
        ToastService.ShowSuccess("Thank you for your supports!");
        return Task.CompletedTask;
    }

    private Task OnChangedAsync()
    {
        return InvokeAsync(StateHasChanged);
    }

    private Task ContextMenuClicked()
    {
        return Task.CompletedTask;
    }

    private void PageHide()
    {
        logger.LogInformation("Page hide");
    }

    private void PageShow()
    {
        logger.LogInformation("Page show");
    }

    private void Offline()
    {
        logger.LogInformation("Page offline");
    }

    private void Online()
    {
        logger.LogInformation("Page online");
    }

    private string EncodeException(Exception e)
    {
        ErrorRecordModel model = new(e);
        logger.LogError(e, e.Message);
        return Uri.EscapeDataString(model.ToJson());
    }
}