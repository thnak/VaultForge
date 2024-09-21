using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

[OllamaTools]
public interface IContentManagementService
{
    [Description("call to check supported content languages")]
    Task<string> GetSupportLanguages([Description("iso language code")] string language, CancellationToken cancellationToken = default);

    [Description("call to get all of the article")]
    public Task<string> GetAllArticle([Description("iso language code")] string? language, CancellationToken cancellationToken = default);

    [Description("call to create new article")]
    Task<string> AddNewContent([Description("title")] string title, [Description("iso language code")] string language, CancellationToken cancellationToken = default);

    [Description("call add summary content to an article")]
    Task<string> AddSummary([Description("title")] string title, [Description("iso language code")] string language, [Description("summary content")] string summary, CancellationToken cancellationToken = default);

    [Description("call to get article")]
    Task<string> GetContent([Description("title")] string title, [Description("iso language code")] string language, CancellationToken cancellationToken = default);

    [Description("call to update html code of the article")]
    Task<string> UpdateHtml([Description("title")] string title, [Description("iso language code")] string language, [Description("HTML code")] string htmlCode, CancellationToken cancellationToken = default);

    [Description("call to update css code of the article")]
    Task<string> UpdateCss([Description("title")] string title, [Description("iso language code")] string language, [Description("css")] string css, CancellationToken cancellationToken = default);

    [Description("call to update javascript code of the article")]
    Task<string> UpdateJavascript([Description("title")] string title, [Description("iso language code")] string language, [Description("javascript")] string javascript, CancellationToken cancellationToken = default);

    [Description("call to get article link")]
    Task<string> GetArticleLink([Description("title")] string title, [Description("iso language code")] string language, CancellationToken cancellationToken = default);
}