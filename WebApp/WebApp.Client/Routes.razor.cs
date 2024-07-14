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
        EventListener.ContextMenuClickedAsync -= ContextMenuClicked;
        EventListener.PageShowEvent -= PageShow;
        EventListener.PageHideEvent -= PageHide;
        EventListener.Online -= Online;
        EventListener.Offline -= Offline;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JsRuntime.InvokeVoidAsync("CloseProgressBar");
            await JsRuntime.InvokeVoidAsync("InitAppEventListener");

            CustomStateContainer.OnChangedAsync += OnChangedAsync;
            EventListener.ContextMenuClickedAsync += ContextMenuClicked;
            EventListener.PageShowEvent += PageShow;
            EventListener.PageHideEvent += PageHide;
            EventListener.Online += Online;
            EventListener.Offline += Offline;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnChangedAsync()
    {
        await InvokeAsync(StateHasChanged);
    }

    private Task ContextMenuClicked()
    {
        Console.WriteLine("Context click");
        return Task.CompletedTask;
    }

    private void PageHide()
    {
        Console.WriteLine("The page has been hidden");
    }

    private void PageShow()
    {
        Console.WriteLine("The page has been displayed");
    }

    private void Offline()
    {
        Console.WriteLine("Offline");
    }

    private void Online()
    {
        Console.WriteLine("Online");
    }

    private string EncodeException(Exception e)
    {
        ErrorRecordModel model = new(e);
        return model.Encode2Base64String();
    }
}