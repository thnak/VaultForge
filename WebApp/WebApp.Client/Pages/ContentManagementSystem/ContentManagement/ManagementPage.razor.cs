using System.Web;
using BusinessModels.Advertisement;
using BusinessModels.Resources;
using BusinessModels.System;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;
using WebApp.Client.Pages.ContentManagementSystem.Editor;
using WebApp.Client.Utils;

namespace WebApp.Client.Pages.ContentManagementSystem.ContentManagement;

public partial class ManagementPage : ComponentBase, IAsyncDisposable, IDisposable
{
    private HubConnection? Hub { get; set; }
    private CancellationTokenSource TokenSource { get; set; } = new();
    private MudDataGrid<ArticleModel> DataGrid { get; set; } = default!;

    private string Title { get; set; } = string.Empty;
    private List<Dictionary<string, string>> Metadata { get; set; } = new();

    #region Dispose

    public async ValueTask DisposeAsync()
    {
        if (Hub != null) await Hub.DisposeAsync();
    }

    public void Dispose()
    {
        TokenSource.Cancel();
        TokenSource.Dispose();
        DataGrid.Dispose();
    }

    #endregion

    #region Initialization

    protected override async Task OnInitializedAsync()
    {
        Title = AppLang.Content_management_system;

        Metadata.Add(new Dictionary<string, string>() { { "name", "description" }, { "content", AppLang.Take_control_of_your_application_s_content_and_functionality_with_a_Headless_CMS } });
        Metadata.Add(new Dictionary<string, string>() { { "name", "keywords" }, { "content", "CMS, content management system, self host" } });
        Metadata.Add(new Dictionary<string, string>() { { "name", "title" }, { "content", AppLang.Content_management_system } });
        Metadata.Add(new Dictionary<string, string>() { { "name", "robots" }, { "content", "max-image-preview:large, index" } });
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Hub = Navigation.ToAbsoluteUri("/PageCreatorHub").InitHub(true);
            Hub.Reconnected += HubOnReconnected;
            Hub.Reconnecting += HubOnReconnecting;
            await Hub.StartAsync();
            await DataGrid.ReloadServerData();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task HubOnReconnecting(Exception? arg)
    {
        ToastService.ShowWarning(AppLang.Reconnecting, TypeClassList.ToastDefaultSetting);
        return Task.CompletedTask;
    }

    private Task HubOnReconnected(string? arg)
    {
        ToastService.ShowSuccess(AppLang.Connected, TypeClassList.ToastDefaultSetting);
        return Task.CompletedTask;
    }

    #endregion


    private async Task<GridData<ArticleModel>> ServerDataFunc(GridState<ArticleModel> arg)
    {
        try
        {
            if (Hub != null)
            {
                SignalRResultValue<ArticleModel> resultValue = await Hub.InvokeAsync<SignalRResultValue<ArticleModel>>("GetAllArticleModel", arg.PageSize, arg.Page, cancellationToken: TokenSource.Token);

                return new GridData<ArticleModel>
                {
                    Items = resultValue.Data,
                    TotalItems = (int)resultValue.Total,
                };
            }
        }
        catch (TaskCanceledException)
        {
        }

        return new GridData<ArticleModel>
        {
            Items = [],
            TotalItems = 0
        };
    }

    private string CreateEditLink(ArticleModel contextItem)
    {
        return Navigation.GetUriWithQueryParameters(PageRoutes.ContentCreator.Index.Src, new Dictionary<string, object?>() { { "edit", true }, { "id", contextItem.Id.ToString() } });
    }

    private string CreatePreviewLink(ArticleModel contextItem)
    {
        return Navigation.GetUriWithQueryParameters(PageRoutes.ContentCreator.Preview.Src, new Dictionary<string, object?>() { { "id", contextItem.Id.ToString() } });
    }

    private async Task DeleteItem(ArticleModel contextItem)
    {
        var param = new DialogParameters<ConfirmDialog>()
        {
            {
                x => x.DataModel, new DialogConfirmDataModel()
                {
                    Icon = Icons.Material.Filled.Delete,
                    Title = AppLang.Warning,
                    Message = $"{AppLang.Delete} {contextItem.Title} ({contextItem.Language})",
                }
            }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("", param);
        var dialogResult = await dialog.Result;
        if (dialogResult is { Canceled: false })
        {
            if (Hub != null)
            {
                var result = await Hub.InvokeAsync<SignalRResult>("DeleteAdvertisement", contextItem.Id.ToString());
                if (result is { Success: true })
                {
                    await DataGrid.ReloadServerData();
                }
            }
        }
    }

    private async Task AddNewArticle()
    {
        var option = new DialogOptions()
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };
        var param = new DialogParameters<ContentCreatorDialog>()
        {
            { x => x.Article, null }
        };
        var dialog = await DialogService.ShowAsync<ContentCreatorDialog>(AppLang.Create_new, param, option);
        var dialogResult = await dialog.Result;
        {
            if (dialogResult is { Canceled: false, Data: ArticleModel })
            {
                await DataGrid.ReloadServerData();
            }
        }
    }

    private async Task CopyLink(ArticleModel contextItem)
    {
        var text = Navigation.GetUriWithQueryParameters(PageRoutes.Advertisement.Index.Src, new Dictionary<string, object?>() { { "content_id", HttpUtility.UrlEncode(contextItem.Title) }, { "lang", contextItem.Language } });
        await JsRuntime.CopyToClipBoard(text);
        ToastService.ShowSuccess(AppLang.Copied);
    }
}