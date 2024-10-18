using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;

namespace WebApp.Client;

public partial class Routes(ILogger<Routes> logger) : ComponentBase, IDisposable
{
    private Guid ResizeId { get; set; }

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
        }

        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(false);
    }

    private void ResizeAction(BrowserViewportEventArgs obj)
    {
        logger.LogInformation($"Height: {obj.BrowserWindowSize.Height}\nWidth: {obj.BrowserWindowSize.Width}");
    }

    private async Task<bool> ScrollToReloadEventAsync()
    {
        var option = new DialogOptions()
        {
            MaxWidth = MaxWidth.Small,
        };
        var dataModel = new DialogConfirmDataModel()
        {
            Icon = "fa-solid fa-rotate-right",
            Color = Color.Secondary,
            Title = AppLang.Reload,
            Fragment = builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(0, AppLang.Do_you_really_want_to_reload_the_page_);
                builder.CloseElement();
            }
        };
        var param = new DialogParameters<ConfirmDialog>()
        {
            { x => x.DataModel, dataModel }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("", param, option);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
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
    }

    private void PageShow()
    {
    }

    private void Offline()
    {
    }

    private void Online()
    {
    }

    private string EncodeException(Exception e)
    {
        ErrorRecordModel model = new(e);
        return model.Encode2Base64String();
    }
}