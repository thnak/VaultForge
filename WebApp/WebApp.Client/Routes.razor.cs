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
        EventListener.ContextMenuClickedAsync -= ContextMenuClicked;
        EventListener.PageShowEvent -= PageShow;
        EventListener.PageHideEvent -= PageHide;
        EventListener.Online -= Online;
        EventListener.Offline -= Offline;
        EventListener.InstalledEventAsync -= InstalledWpa;
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
            EventListener.InstalledEventAsync += InstalledWpa;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task InstalledWpa()
    {
        ToastService.ShowSuccess("Thank you for your supports!");
        return Task.CompletedTask;
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
        Console.WriteLine(AppLang.Routes_PageHide_The_page_has_been_hidden);
    }

    private void PageShow()
    {
        Console.WriteLine(AppLang.Routes_PageShow_The_page_has_been_displayed);
    }

    private void Offline()
    {
        Console.WriteLine(AppLang.Routes_Offline_Offline);
    }

    private void Online()
    {
        Console.WriteLine(AppLang.Routes_Online_Online);
    }

    private string EncodeException(Exception e)
    {
        ErrorRecordModel model = new(e);
        return model.Encode2Base64String();
    }
}