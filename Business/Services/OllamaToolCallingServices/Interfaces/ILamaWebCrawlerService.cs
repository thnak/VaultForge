using System.ComponentModel;
using Ollama;

namespace Business.Services.OllamaToolCallingServices.Interfaces;

// [OllamaTools]
public interface ILamaWebCrawlerService
{
    [Description("collects website data from the provided website url")]
    public Task<string> CrawlAsync([Description("web site to craw")] string url, CancellationToken token = default);
}