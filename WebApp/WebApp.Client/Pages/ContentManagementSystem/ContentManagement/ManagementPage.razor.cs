using BusinessModels.Advertisement;
using BusinessModels.Converter;
using BusinessModels.Resources;
using BusinessModels.System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;
using WebApp.Client.Pages.ContentManagementSystem.Editor;

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


        Hub = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/PageCreatorHub"))
            .AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new ObjectIdConverter()); })
            .Build();

        await Hub.StartAsync();
        await DataGrid.ReloadServerData();
        await base.OnInitializedAsync();
    }

    #endregion


    private async Task<GridData<ArticleModel>> ServerDataFunc(GridState<ArticleModel> arg)
    {
        try
        {
            if (Hub != null)
            {
                SignalRResult<ArticleModel> result = await Hub.InvokeAsync<SignalRResult<ArticleModel>>("GetAllArticleModel", arg.PageSize, arg.Page, cancellationToken: TokenSource.Token);

                return new GridData<ArticleModel>
                {
                    Items = result.Data,
                    TotalItems = (int)result.Total,
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
            await DataGrid.ReloadServerData();
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
}