using System.Collections.Concurrent;
using Abot2.Crawler;
using Abot2.Poco;
using Business.Services.OllamaToolCallingServices.Interfaces;

namespace Business.Services.OllamaToolCallingServices;

public class LamaWebCrawlerService : ILamaWebCrawlerService
{
    public async Task<string> CrawlAsync(string url, CancellationToken token = default)
    {
        var cancelTokenSource = new CancellationTokenSource();

        try
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 1, //Only crawl 10 pages
                MinCrawlDelayPerDomainMilliSeconds = 3000,
                MaxConcurrentThreads = 2,
                MaxMemoryUsageInMb = 100
            };
            var crawler = new PoliteWebCrawler(config);
            ConcurrentBag<string> text = new();
            crawler.PageCrawlCompleted += CrawlerOnPageCrawlCompleted;

            void CrawlerOnPageCrawlCompleted(object? sender, PageCrawlCompletedArgs e)
            {
                try
                {
                    if (e.CrawledPage.HttpResponseMessage.IsSuccessStatusCode)
                    {
                        var rawPageText = e.CrawledPage.Content.Text;
                        text.Add(rawPageText);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            await crawler.CrawlAsync(new Uri(url), cancelTokenSource);
            var mess = string.Join(", ", text);
            return mess;
        }
        catch (Exception exception)
        {
            return exception.Message;
        }
        finally
        {
            cancelTokenSource.Dispose();
        }
    }
}