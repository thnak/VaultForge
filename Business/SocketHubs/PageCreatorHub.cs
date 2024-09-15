using Business.Business.Interfaces.Advertisement;
using BusinessModels.Advertisement;
using BusinessModels.System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace Business.SocketHubs;

public class PageCreatorHub(IMemoryCache memoryCache, IAdvertisementBusinessLayer businessLayer) : Hub
{
    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    #region Hub Self Methods

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();
        return base.OnDisconnectedAsync(exception);
    }

    public override Task OnConnectedAsync()
    {
        CancellationTokenSource = new CancellationTokenSource();
        return base.OnConnectedAsync();
    }

    #endregion


    public async Task<bool> CheckExistById(string id)
    {
        await foreach (var _ in businessLayer.FindProjectAsync(id, cancellationToken: CancellationTokenSource.Token))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CheckExistByTitleAndLanguage(string title, string language)
    {
        await foreach (var article in businessLayer.FindProjectAsync(title, cancellationToken: CancellationTokenSource.Token))
        {
            if (article.Language == language)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<SignalRResultValue<ArticleModel>> GetAllArticleModel(int pageSize, int currentPage)
    {
        var re = await businessLayer.GetAllAsync(currentPage, pageSize, CancellationTokenSource.Token);
        return new SignalRResultValue<ArticleModel>()
        {
            Data = re.Item1,
            Total = re.Item2,
            Success = true
        };
    }

    public async Task<SignalRResult> CreateAdvertisement(ArticleModel article)
    {
        var result = await businessLayer.CreateAsync(article, CancellationTokenSource.Token);
        if (!result.Item1)
        {
            result = await businessLayer.UpdateAsync(article, CancellationTokenSource.Token);
            return new SignalRResult() { Message = result.Item2, Success = result.Item1 };
        }

        memoryCache.Set($"{nameof(ArticleModel)}{article.Title}", article, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1) });

        return new SignalRResult() { Message = result.Item2, Success = result.Item1 };
    }


    public async Task SendMessage(ArticleModel message)
    {
        _ = Task.Run(() => CreateAdvertisement(message).ConfigureAwait(false));
        await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceiveMessage", message, CancellationTokenSource.Token);
    }

    public async Task GetMessages(string articleId)
    {
        var data = memoryCache.GetOrCreate<ArticleModel?>($"{nameof(ArticleModel)}{articleId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1);
            return businessLayer.Get(articleId);
        });

        await Clients.Caller.SendAsync("ReceiveMessage", data, CancellationTokenSource.Token);
    }
}