using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace WebApp.Client;

public partial class Routes : ComponentBase, IDisposable
{
    public void Dispose()
    {
        CustomStateContainer.OnChangedAsync -= OnChangedAsync;
        EventListener.Dispose();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("CloseProgressBar").ConfigureAwait(false);
            await JsRuntime.InvokeVoidAsync("InitAppEventListener").ConfigureAwait(false);

            CustomStateContainer.OnChangedAsync += OnChangedAsync;
            EventListener.ContextMenuClickedAsync += ContextMenuClicked;
            EventListener.PageShowEvent += PageShow;
            EventListener.PageHideEvent += PageHide;
            EventListener.Online += Online;
            EventListener.Offline += Offline;
            EventListener.InstalledEventAsync += InstalledWpa;
        }

        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(false);
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