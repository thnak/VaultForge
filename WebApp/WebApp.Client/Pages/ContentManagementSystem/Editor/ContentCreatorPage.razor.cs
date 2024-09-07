using System.Globalization;
using BlazorMonaco;
using BlazorMonaco.Editor;
using BusinessModels.Advertisement;
using BusinessModels.Converter;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace WebApp.Client.Pages.ContentManagementSystem.Editor;

public partial class ContentCreatorPage : ComponentBase, IDisposable, IAsyncDisposable
{
    #region Parameters

    [SupplyParameterFromQuery(Name = "edit")]
    public bool? Editable { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    public string? ContentId { get; set; } = string.Empty;

    #endregion

    private string Title { get; set; } = string.Empty;
    private List<Dictionary<string, string>> MetaData { get; set; } = [];
    private StandaloneCodeEditor Editor { get; set; } = default!;
    private HubConnection? HubConnection { get; set; }
    private CancellationTokenSource TokenSource { get; set; } = new();


    private ArticleModel Article { get; set; } = new();

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            HubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/PageCreatorHub"))
                .AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new ObjectIdConverter()); })
                .Build();

            HubConnection.On<ArticleModel>("ReceiveMessage", ReceiveArticleData);
            await HubConnection.StartAsync();
            if (ContentId != null)
            {
                _ = Task.Run(async () => await HubConnection.InvokeAsync("ReceiveMessage", ContentId));
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private Task ReceiveArticleData(ArticleModel arg)
    {
        Article = arg;
        Title = arg.Title;
        return Editor.SetValue(arg.Content);
    }


    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Theme = "Visual Studio Dark",
            Language = "html",
            Value = "",
            AcceptSuggestionOnCommitCharacter = true,
            InlineCompletionsAccessibilityVerbose = true,
            DetectIndentation = true,
            Suggest = new SuggestOptions() { ShowClasses = true, ShowColors = true, ShowConstructors = true, ShowConstants = true, ShowDeprecated = true, ShowIcons = true, ShowWords = true }
        };
    }

    private Task KeyUp(KeyboardEvent arg)
    {
        return HandleCode();
    }


    private async Task HandleCode()
    {
        var text = await Editor!.GetValue();

        if (HubConnection != null)
        {
            Article.Content = text;
            await HubConnection.SendAsync("SendMessage", Article);
        }
    }


    public void Dispose()
    {
        Editor.Dispose();
        TokenSource.Cancel();
        TokenSource.Dispose();
    }

    #region Dispose

    public async ValueTask DisposeAsync()
    {
        if (HubConnection != null) await HubConnection.DisposeAsync();
    }

    private async Task AddNewArticle()
    {
        await OpenEditDialog(null);
    }

    #endregion


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
                Article = model;
                Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(Navigation.Uri, new Dictionary<string, object?>() { { "id", model.Id.ToString() }, { "edit", true } }));
            }
        }
    }
}