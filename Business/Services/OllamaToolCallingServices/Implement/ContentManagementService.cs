using System.Text;
using Business.Business.Interfaces.Advertisement;
using Business.Services.OllamaToolCallingServices.Interfaces;
using BusinessModels.Advertisement;
using BusinessModels.General.Update;
using BusinessModels.Resources;
using BusinessModels.Utils;

namespace Business.Services.OllamaToolCallingServices.Implement;

public class ContentManagementService(IAdvertisementBusinessLayer businessLayer) : IContentManagementService
{
    public Task<string> GetSupportLanguages(string language, CancellationToken cancellationToken = default)
    {
        var allowed = string.Join(", ", AllowedCulture.SupportedCultures.Select(x => x.Name));
        var isSupported = AllowedCulture.SupportedCultures.Any(x => x.Name == language);
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append($"Supported languages: {allowed}. ");

        if (isSupported)
            stringBuilder.AppendLine($"{language} is supported.");
        else
            stringBuilder.AppendLine($"{language} is not supported.");

        return Task.FromResult(stringBuilder.ToString());
    }

    public async Task<string> GetAllArticle(string language = "en-US", CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<ArticleModel> cursor = businessLayer.Where(x => x.Language == language, cancellationToken, model => model.Id, model => model.Title, model => model.Language, model => model.Summary, model => model.ModifiedTime);

        List<ArticleModel> articles = new List<ArticleModel>();
        await foreach (var item in cursor)
        {
            articles.Add(item);
        }

        return articles.ToJson();
    }

    public async Task<string> AddNewContent(string title, string language = "en-US", CancellationToken cancellationToken = default)
    {
        if (!CheckLanguage(language))
        {
            var allowed = string.Join(", ", AllowedCulture.SupportedCultures.Select(x => x.Name));
            return $"the current language is not supported. Supported language is {allowed}. Tell user choose one and try again.";
        }

        var model = new ArticleModel()
        {
            Title = title,
            Language = language,
        };
        var result = await businessLayer.CreateAsync(model, cancellationToken);
        if (result.IsSuccess)
        {
            return $"Added new article with ID {model.Id}. Remember it to use this article again.";
        }

        model = businessLayer.Get(title, language);
        if (model != null)
        {
            return $"The article with ID {model.Id} already exists. Please choose another one or try again. Or you can update the content of this article.";
        }

        return $"Add failed for article with title {title} and language {language}. Reason: {result.Message}";
    }

    public async Task<string> DeleteArticle(string id, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(id);
        if (article == null)
            return "The Article could not be found. Ask user to provide correct article ID and try again. Ask user provide correct article ID and try again.";
        var result = await businessLayer.DeleteAsync(id, cancellationToken);
        return result.IsSuccess ? $"deleted successfully for article {article.Id}." : $"delete failed for article {article.Id}. Ask user to provide correct article ID and try again.";
    }

    public async Task<string> AddSummary(string id, string summary, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(id);
        if (article == null)
            return "The Article could not be found. Ask user to provide correct article ID and try again.";

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.Summary, summary }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.IsSuccess ? $"Update successfully for article {article.Id}." : $"Update failed for article {article.Id}. Reason: {result.Message}";
    }

    public async Task<string> GetContent(string id, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(id);
        if (article == null)
            return await Task.FromResult("The article could not be found. Ask user to provide correct article ID and try again.");
        return await Task.FromResult($"""Use "{article.ToJson<ArticleModel>()}" to response to user""");
    }

    public async Task<string> UpdateHtml(string id, string htmlCode, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(id);
        if (article == null)
            return "Article could not be found. Ask user to provide correct article ID and try again. Ask user to provide correct article ID and try again.";

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.HtmlSheet, htmlCode }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.IsSuccess ? $"Update successfully for article {article.Id}." : $"Update failed for article {article.Id}. Reason: {result.Message}";
    }

    public async Task<string> UpdateCss(string id, string css, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(id);
        if (article == null)
            return "Article could not be found. Ask user to provide correct article ID and try again.";

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.StyleSheet, css }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.IsSuccess ? $"Update successfully for article {article.Id}." : $"Update failed for article {article.Id}. Reason: {result.Message}";
    }

    public async Task<string> UpdateJavascript(string id, string javascript, CancellationToken cancellationToken = default)
    {
        var article = businessLayer.Get(id);
        if (article == null)
            return "Article could not be found. Ask user to provide correct article ID and try again.";

        FieldUpdate<ArticleModel> fieldUpdate = new FieldUpdate<ArticleModel>()
        {
            { x => x.JavaScriptSheet, javascript }
        };

        var result = await businessLayer.UpdateAsync(article.Id.ToString(), fieldUpdate, cancellationToken);
        return result.IsSuccess ? $"Update successfully for article {article.Id}." : $"Update failed for article {article.Id}. Reason: {result.Message}";
    }

    public async Task<string> GetArticleLink(string title, string language, CancellationToken cancellationToken = default)
    {
        if (!CheckLanguage(language))
            return "the current language is not supported. please check the supported languages.";
        var article = businessLayer.Get(title, language);
        if (article == null)
            return await Task.FromResult(AppLang.Article_does_not_exist);
        return await Task.FromResult($"/?id={article.Id}");
    }

    private bool CheckLanguage(string lang)
    {
        return AllowedCulture.SupportedCultures.Any(x => x.Name == lang);
    }
}