﻿using System.Globalization;
using BusinessModels.Advertisement;
using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.ContentManagementSystem.Preview;

public partial class PreviewContentPage : ComponentBase, IDisposable, IAsyncDisposable
{
    #region Parameters

    [SupplyParameterFromQuery(Name = "id")]
    public string? ContentId { get; set; } = string.Empty;

    #endregion


    #region Fields

    private HubConnection? HubConnection { get; set; }
    private CancellationTokenSource TokenSource { get; set; } = new();
    private string Title { get; set; } = string.Empty;
    private List<Dictionary<string, string>> MetaData { get; set; } = [];

    private RenderFragment? Content { get; set; }

    #endregion

    #region Dispose

    public void Dispose()
    {
        TokenSource.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (HubConnection != null) await HubConnection.DisposeAsync();
    }

    #endregion


    #region Init

    protected override void OnInitialized()
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        Title = AppLang.Content_creator;
        switch (culture)
        {
            case "vi-VN":
                MetaData.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", "Biên soạn nội dung trình bày của riêng bạn" } });

                break;
            case "en-US":
                MetaData.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", "Compile your own presentation content" } });
                break;
        }

        base.OnInitialized();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HubConnection = Navigation.ToAbsoluteUri("/PageCreatorHub").InitConnection();
            HubConnection.On<ArticleModel>("ReceiveMessage", ReceiveArticleData);
            HubConnection.Reconnected += HubConnectionOnReconnected;
            HubConnection.Reconnecting += HubConnectionOnReconnecting;
            HubConnection.StartAsync();
            if (ContentId != null)
            {
                HubConnection.InvokeAsync("GetMessages", ContentId);
            }
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    private Task HubConnectionOnReconnecting(Exception? arg)
    {
        ToastService.ShowWarning(AppLang.Reconnecting, TypeClassList.ToastDefaultSetting);
        return InvokeAsync(StateHasChanged);
    }

    private async Task HubConnectionOnReconnected(string? arg)
    {
        ToastService.ShowSuccess(AppLang.Connected, TypeClassList.ToastDefaultSetting);
        await HubConnection!.InvokeAsync("GetMessages", ContentId);
    }

    private Task ReceiveArticleData(ArticleModel arg)
    {
        Title = arg.Title;
        MetaData.Clear();
        MetaData.Add(new Dictionary<string, string>() { { "name", "title" }, { "content", arg.Title } });
        MetaData.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", arg.Summary } });
        MetaData.Add(new Dictionary<string, string>() { { "name", "keywords" }, { "content", string.Join(", ", arg.Keywords) } });
        MetaData.Add(new Dictionary<string, string>() { { "name", "image" }, { "content", arg.Image } });

        int index = 0;
        Content = builder =>
        {
            builder.OpenElement(index++, "style");
            builder.AddMarkupContent(0, arg.StyleSheet);
            builder.CloseElement();

            builder.AddMarkupContent(index++, arg.HtmlSheet);

            builder.OpenElement(index++, "script");
            builder.AddMarkupContent(0, arg.JavaScriptSheet);
            builder.CloseElement();
        };
        return InvokeAsync(StateHasChanged);
    }

    #endregion
}