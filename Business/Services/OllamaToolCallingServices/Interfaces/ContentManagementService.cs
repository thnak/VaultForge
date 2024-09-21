using Business.Business.Interfaces.Advertisement;
using Business.Models;
using BusinessModels.Advertisement;
using BusinessModels.Resources;
using BusinessModels.Utils;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

public class ContentManagementService(IAdvertisementBusinessLayer businessLayer) : IContentManagementService
{
    public Task<string> GetSupportLanguages(string language, CancellationToken cancellationToken = default)
    {
        var allowed = AllowedCulture.SupportedCultures.Select(x => x.Name).ToList().ToJson();
        var isSupported = AllowedCulture.SupportedCultures.Any(x => x.Name == language);
        var result = new { Supported = allowed, IsSupported = isSupported };
        return Task.FromResult(result.ToJson());
    }

    public async Task<string> GetAllArticle(string? language, CancellationToken cancellationToken = default)
    {
        var cursor = businessLayer.Where(x => x.Language == language, cancellationToken, model => model.Id, model => model.Title, model => model.Language, model => model.Summary, model => model.ModifiedTime);
        List<ArticleModel> articles = new List<ArticleModel>();
        await foreach (var item in cursor)
        {
            articles.Add(item);
        }

        return articles.ToJson();
    }

    public async Task<string> AddNewContent(string title, string language, CancellationToken cancellationToken = default)
    {
        var result = await businessLayer.CreateAsync(new ArticleModel()
        {
            Title = title,
            Language = language,
        }, cancellationToken);
        return result.ToJson();
    }

    public async Task<string> AddSummary(string title, string language, string summary, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(title, language);
        if (article == null)
            return AppLang.Article_does_not_exist;

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.Summary, summary }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.Item2;
    }

    public Task<string> GetContent(string title, string language, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(title, language);
        if (article == null)
            return Task.FromResult(AppLang.Article_does_not_exist);
        return Task.FromResult(article.ToJson());
    }

    public async Task<string> UpdateHtml(string title, string language, string htmlCode, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(title, language);
        if (article == null)
            return AppLang.Article_does_not_exist;

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.HtmlSheet, htmlCode }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.Item2;
    }

    public async Task<string> UpdateCss(string title, string language, string css, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(title, language);
        if (article == null)
            return AppLang.Article_does_not_exist;

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.StyleSheet, css }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.Item2;
    }

    public async Task<string> UpdateJavascript(string title, string language, string javascript, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(title, language);
        if (article == null)
            return AppLang.Article_does_not_exist;

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.JavaScriptSheet, javascript }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.Item2;
    }

    public Task<string> GetArticleLink(string title, string language, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(title, language);
        if (article == null)
            return Task.FromResult(AppLang.Article_does_not_exist);
        return Task.FromResult($"/?id={article.Id}");
    }
}