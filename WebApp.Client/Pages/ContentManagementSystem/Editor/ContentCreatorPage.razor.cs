using System.Globalization;
using BusinessModels.Advertisement;
using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using WebApp.Client.Components.CodeEditor;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.ContentManagementSystem.Editor;

public partial class ContentCreatorPage : ComponentBase, IDisposable, IAsyncDisposable
{
    #region Parameters

    [SupplyParameterFromQuery(Name = "edit")]
    public bool? Editable { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    public string? ContentId { get; set; } = string.Empty;

    #endregion

    #region Fields

    private string Title { get; set; } = string.Empty;
    private List<Dictionary<string, string>> MetaData { get; set; } = [];
 

    private HubConnection? HubConnection { get; set; }
    private CancellationTokenSource TokenSource { get; set; } = new();

    private static ArticleModel Article { get; set; } = new();

    private bool Loading { get; set; } = true;

    private MonacoCodeEditor? MonacoCodeEditor { get; set; } 
    
    #endregion


    #region Init

    protected override void OnInitialized()
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        Title = AppLang.Content_editor;
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitHub();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task InitHub()
    {
        HubConnection = Navigation.ToAbsoluteUri("/PageCreatorHub").InitConnection();
        await JsRuntime.AddScriptResource();
        HubConnection.On<ArticleModel>("ReceiveMessage", ReceiveArticleData);
        HubConnection.Reconnected += HubConnectionOnReconnected;
        HubConnection.Reconnecting += HubConnectionOnReconnecting;
        await HubConnection.StartAsync();
        await HubConnection.InvokeAsync("GetMessages", ContentId);
    }

    private Task HubConnectionOnReconnecting(Exception? arg)
    {
        Loading = true;
        ToastService.ShowWarning(AppLang.Reconnecting, TypeClassList.ToastDefaultSetting);
        return InvokeAsync(StateHasChanged);
    }

    private async Task HubConnectionOnReconnected(string? arg)
    {
        ToastService.ShowSuccess(AppLang.Connected, TypeClassList.ToastDefaultSetting);
        await HubConnection!.InvokeAsync("GetMessages", ContentId);
    }

    private async Task ReceiveArticleData(ArticleModel arg)
    {
        Article = arg;
        Title = arg.Title;
        MetaData.Clear();
        MetaData.Add(new Dictionary<string, string>() { { "name", "title" }, { "content", arg.Title } });
        MetaData.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", arg.Summary } });
        MetaData.Add(new Dictionary<string, string>() { { "name", "keywords" }, { "content", string.Join(", ", arg.Keywords) } });
        MetaData.Add(new Dictionary<string, string>() { { "name", "image" }, { "content", arg.Image } });
        if (MonacoCodeEditor != null) await MonacoCodeEditor.SetValue(arg);
        Loading = false;
        await InvokeAsync(StateHasChanged);
    }

    #endregion


    #region Init editor

    private async Task HandleSend()
    {
        if (HubConnection is { State: HubConnectionState.Connected })
        {
            await HubConnection.SendAsync("SendMessage", Article);
        }
    }

    #endregion


    #region Dispose

    public async ValueTask DisposeAsync()
    {
        if (HubConnection != null) await HubConnection.DisposeAsync();
        
    }

    public void Dispose()
    {
        if (MonacoCodeEditor != null) MonacoCodeEditor.Dispose();
        TokenSource.Cancel();
        TokenSource.Dispose();
    }

    #endregion

    private async Task AddNewArticle()
    {
        await OpenEditDialog(Article);
    }

    private async Task OpenEditDialog(ArticleModel? articleModel)
    {
        var option = new DialogOptions()
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };
        var param = new DialogParameters<ContentCreatorDialog>()
        {
            { x => x.Article, articleModel }
        };
        var dialog = await DialogService.ShowAsync<ContentCreatorDialog>(articleModel == null ? AppLang.Create_new : AppLang.Edit, param, option);
        var dialogResult = await dialog.Result;
        {
            if (dialogResult is { Canceled: false, Data: ArticleModel model })
            {
                Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(Navigation.Uri, new Dictionary<string, object?>() { { "id", model.Id.ToString() }, { "edit", Editable } }), new NavigationOptions() { ForceLoad = true });
            }
        }
    }

    private Task ArticalChanged(ArticleModel arg)
    {
        Article = arg;
        return HandleSend();
    }
}