using BusinessModels.Advertisement;
using BusinessModels.Converter;
using BusinessModels.Resources;
using BusinessModels.System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using WebApp.Client.Components.ConfirmDialog;
using WebApp.Client.Models;

namespace WebApp.Client.Pages.ContentManagementSystem.ContentManagement;

public partial class ManagementPage : ComponentBase, IAsyncDisposable, IDisposable
{
    private HubConnection? Hub { get; set; }
    private CancellationTokenSource TokenSource { get; set; } = new();
    private MudDataGrid<ArticleModel> DataGrid { get; set; } = default!;

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
}