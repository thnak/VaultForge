using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface IContentManagementService
{
    [Description("call to check supported iso code languages")]
    Task<string> GetSupportLanguages([Description("iso language code")] string language, CancellationToken cancellationToken = default);

    [Description("call to get all of the article")]
    public Task<string> GetAllArticle([Description("iso language code")] string? language, CancellationToken cancellationToken = default);

    [Description("call to create new article")]
    Task<string> AddNewContent([Description("title")] string title, [Description("iso language code")] string language, CancellationToken cancellationToken = default);

    [Description("call add summary content of the article with the specified title and language")]
    Task<string> AddSummary([Description("title")] string title, [Description("iso language code")] string language, [Description("summary content")] string summary, CancellationToken cancellationToken = default);

    [Description("call to get article by title and languages")]
    Task<string> GetContent([Description("title")] string title, [Description("iso language code")] string language, CancellationToken cancellationToken = default);

    [Description("call to update the html content of the article with the specified title and language")]
    Task<string> UpdateHtml([Description("title")] string title, [Description("iso language code")] string language, [Description("HTML code")] string htmlCode, CancellationToken cancellationToken = default);

    [Description("call to update the css content of the article with the specified title and language")]
    Task<string> UpdateCss([Description("title")] string title, [Description("iso language code")] string language, [Description("css")] string css, CancellationToken cancellationToken = default);

    [Description("call to update the javascript content of the article with the specified title and language")]
    Task<string> UpdateJavascript([Description("title")] string title, [Description("iso language code")] string language, [Description("javascript")] string javascript, CancellationToken cancellationToken = default);

    [Description("call to get article link of the article with the specified title and language")]
    Task<string> GetArticleLink([Description("title")] string title, [Description("iso language code")] string language, CancellationToken cancellationToken = default);
}