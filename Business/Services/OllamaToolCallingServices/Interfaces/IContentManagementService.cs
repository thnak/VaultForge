using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

// [OllamaTools]
public interface IContentManagementService
{
    [Description("check supported iso code languages of article handler")]
    Task<string> GetSupportLanguages([Description("iso language code. default en-US")] string language = "en-US", CancellationToken cancellationToken = default);

    [Description("get all of the article")]
    public Task<string> GetAllArticle([Description("iso language code. default en-US")] string language = "en-US", CancellationToken cancellationToken = default);

    [Description("create new article")]
    Task<string> AddNewContent([Description("title")] string title, [Description("iso language code. default en-US")] string language = "en-US", CancellationToken cancellationToken = default);

    [Description("delete article")]
    Task<string> DeleteArticle([Description("article id")] string id, CancellationToken cancellationToken = default);

    [Description("update summary of the article")]
    Task<string> AddSummary([Description("article id")] string id, [Description("summary content")] string summary, CancellationToken cancellationToken = default);

    [Description("get article by title and languages")]
    Task<string> GetContent([Description("article id")] string id, CancellationToken cancellationToken = default);

    [Description("update the whole html content of the article")]
    Task<string> UpdateHtml([Description("article id")] string id, [Description("HTML code")] string htmlCode, CancellationToken cancellationToken = default);

    [Description("update the whole css content of the article with the specified title and language")]
    Task<string> UpdateCss([Description("article id")] string id, [Description("css code")] string css, CancellationToken cancellationToken = default);

    [Description("update the whole javascript content of the article")]
    Task<string> UpdateJavascript([Description("article id")] string id, [Description("javascript code")] string javascript, CancellationToken cancellationToken = default);

    [Description("get article link of the article with the specified title and language")]
    Task<string> GetArticleLink([Description("title")] string title, [Description("iso language code")] string language, CancellationToken cancellationToken = default);
}