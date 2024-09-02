using Abot2.Crawler;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;

public class LamaWebCrawlerService : ILamaWebCrawlerService
{
    public async Task<string> CrawlAsync(string url, CancellationToken token = default)
    {
        var cancelTokenSource = new CancellationTokenSource();
        var crawler = new PoliteWebCrawler();
        var result = await crawler.CrawlAsync(new Uri(url), cancelTokenSource);
        return string.Empty;
    }
}