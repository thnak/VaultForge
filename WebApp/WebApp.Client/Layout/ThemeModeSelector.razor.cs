using Microsoft.AspNetCore.Components;
using MudBlazor;
using WebApp.Client.Assets;
using WebApp.Client.Utils;

namespace WebApp.Client.Layout;

public partial class ThemeModeSelector : ComponentBase, IDisposable
{
    private bool? IsDarkMode { get; set; }
    private string? Theme { get; set; }

    private Dictionary<string, MudTheme> MudThemes { get; set; } = new()
    {
        { nameof(StaticThemes.Default), StaticThemes.Default },
        { nameof(StaticThemes.Zephyrtheme), StaticThemes.Zephyrtheme }
    };

    public void Dispose()
    {
        CustomStateContainer.OnChanged -= StateHasChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            CustomStateContainer.OnChanged += StateHasChanged;
            Theme = MudThemes.First().Key;
            var isDarkMode = await JsRuntime.GetLocalStorage(nameof(CustomStateContainer.IsDarkMode));
            if (isDarkMode != null)
            {
                IsDarkMode = bool.Parse(isDarkMode);
                CustomStateContainer.IsDarkMode = bool.Parse(isDarkMode);
            }
            else
            {
                IsDarkMode = null;
            }
        }
    }

    private Task WatchSystemPreference(bool mode)
    {
        if (IsDarkMode == null)
        {
            CustomStateContainer.IsDarkMode = mode;
            InvokeAsync(StateHasChanged);
        }

        return Task.CompletedTask;
    }

    private async Task ThemeModeChanged()
    {
        switch (IsDarkMode)
        {
            case null:
                IsDarkMode = true;
                CustomStateContainer.IsDarkMode = IsDarkMode.Value;
                await JsRuntime.SetLocalStorage(nameof(CustomStateContainer.IsDarkMode), IsDarkMode.Value);
                break;
            case true:
                IsDarkMode = false;
                CustomStateContainer.IsDarkMode = IsDarkMode.Value;
                await JsRuntime.SetLocalStorage(nameof(CustomStateContainer.IsDarkMode), IsDarkMode.Value);
                break;
            case false:
                IsDarkMode = null;
                await JsRuntime.RemoveLocalStorage(nameof(CustomStateContainer.IsDarkMode));
                break;
        }



        await InvokeAsync(StateHasChanged);
    }

    private async Task ThemeValueChanged(string arg)
    {
        Theme = arg;
        CustomStateContainer.MudTheme = MudThemes[arg];
        await InvokeAsync(StateHasChanged);
    }
}