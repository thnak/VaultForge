using System.Globalization;
using BlazorMonaco;
using BlazorMonaco.Editor;
using BusinessModels.Advertisement;
using BusinessModels.Converter;
using BusinessModels.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
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
    private StandaloneCodeEditor HtmlEditor { get; set; } = default!;
    private StandaloneCodeEditor CssEditor { get; set; } = default!;
    private StandaloneCodeEditor JavascriptEditor { get; set; } = default!;

    private HubConnection? HubConnection { get; set; }
    private CancellationTokenSource TokenSource { get; set; } = new();


    private static ArticleModel Article { get; set; } = new();

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
            HubConnection = new HubConnectionBuilder()
                .WithUrl(Navigation.ToAbsoluteUri("/PageCreatorHub"))
                .AddJsonProtocol(options => { options.PayloadSerializerOptions.Converters.Add(new ObjectIdConverter()); })
                .Build();

            HubConnection.On<ArticleModel>("ReceiveMessage", ReceiveArticleData);
            await HubConnection.StartAsync();
            if (ContentId != null)
            {
                _ = Task.Run(async () => await HubConnection.InvokeAsync("GetMessages", ContentId));
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable]
    public static Task<string> GetCurrentStyle()
    {
        return Task.FromResult(Article.StyleSheet);
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

        await HtmlEditor.SetValue(arg.HtmlSheet);
        await CssEditor.SetValue(arg.StyleSheet);
        await JavascriptEditor.SetValue(arg.JavaScriptSheet);
    }


    private StandaloneEditorConstructionOptions HtmlEditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Theme = "vs-dark",
            Language = "html",
            Value = "",
            AcceptSuggestionOnCommitCharacter = true,
            InlineCompletionsAccessibilityVerbose = true,
            DetectIndentation = true,
            Suggest = new SuggestOptions() { ShowClasses = true, ShowColors = true, ShowConstructors = true, ShowConstants = true, ShowDeprecated = true, ShowIcons = true, ShowWords = true },
        };
    }

    private StandaloneEditorConstructionOptions CssEditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Theme = "vs-dark",
            Language = "css",
            Value = "",
            AcceptSuggestionOnCommitCharacter = true,
            InlineCompletionsAccessibilityVerbose = true,
            DetectIndentation = true,
            Suggest = new SuggestOptions() { ShowClasses = true, ShowColors = true, ShowConstructors = true, ShowConstants = true, ShowDeprecated = true, ShowIcons = true, ShowWords = true },
        };
    }

    private StandaloneEditorConstructionOptions JavascriptEditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Theme = "vs-dark",
            Language = "javascript",
            Value = "",
            AcceptSuggestionOnCommitCharacter = true,
            InlineCompletionsAccessibilityVerbose = true,
            DetectIndentation = true,
            Suggest = new SuggestOptions() { ShowClasses = true, ShowColors = true, ShowConstructors = true, ShowConstants = true, ShowDeprecated = true, ShowIcons = true, ShowWords = true },
        };
    }

    private Task KeyHtmlUp(KeyboardEvent arg)
    {
        return HandleHtmlCode();
    }


    private async Task HandleHtmlCode()
    {
        var text = await HtmlEditor!.GetValue();
        if (HubConnection != null)
        {
            Article.HtmlSheet = text;
            await HubConnection.SendAsync("SendMessage", Article);
        }
    }

    private Task KeyCssUp(KeyboardEvent arg)
    {
        return HandleCssCode();
    }


    private async Task HandleCssCode()
    {
        var text = await CssEditor!.GetValue();
        if (HubConnection != null)
        {
            Article.StyleSheet = text;
            await HubConnection.SendAsync("SendMessage", Article);
        }
    }

    private Task KeyJavascriptUp(KeyboardEvent arg)
    {
        return HandleJavascriptCode();
    }


    private async Task HandleJavascriptCode()
    {
        var text = await CssEditor!.GetValue();
        if (HubConnection != null)
        {
            Article.JavaScriptSheet = text;
            await HubConnection.SendAsync("SendMessage", Article);
        }
    }


    public void Dispose()
    {
        HtmlEditor.Dispose();
        CssEditor.Dispose();
        JavascriptEditor.Dispose();

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
        await OpenEditDialog(Article);
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
                Navigation.NavigateTo(Navigation.GetUriWithQueryParameters(Navigation.Uri, new Dictionary<string, object?>() { { "id", model.Id.ToString() }, { "edit", Editable } }), new NavigationOptions() { ForceLoad = true });
            }
        }
    }
}