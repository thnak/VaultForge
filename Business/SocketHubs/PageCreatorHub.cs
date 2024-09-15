using Business.Business.Interfaces.Advertisement;
using BusinessModels.Advertisement;
using BusinessModels.System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace Business.SocketHubs;

public class PageCreatorHub(IMemoryCache memoryCache, IAdvertisementBusinessLayer businessLayer) : Hub
{
    private const string CacheKey = "PageCreatorHub";

    private CancellationTokenSource CancellationTokenSource { get; set; } = new();

    #region Hub Self Methods

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();
        RemoveListeners();
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

        memoryCache.Set($"{nameof(ArticleModel)}{article.Id}", article, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1) });

        return new SignalRResult() { Message = result.Item2, Success = result.Item1 };
    }


    public async Task SendMessage(ArticleModel message)
    {
        _ = Task.Run(() => CreateAdvertisement(message).ConfigureAwait(false));
        string articleId = message.Id.ToString();
        var listeners = GetListeners(articleId).Where(x => x != Context.ConnectionId).ToArray();
        await Clients.Users(listeners).SendAsync("ReceiveMessage", message, CancellationTokenSource.Token);
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

    public async Task<SignalRResult> DeleteAdvertisement(string articleId)
    {
        var result = businessLayer.Delete(articleId);
        memoryCache.Remove($"{nameof(ArticleModel)}{articleId}");
        var listener = GetListeners(articleId);
        await Clients.Users(listener).SendAsync("ReceiveMessage", new ArticleModel(), CancellationTokenSource.Token);
        return new SignalRResult() { Message = result.Item2, Success = result.Item1 };
    }

    private string[] GetListeners(string articleId)
    {
        var listener = memoryCache.GetOrCreate(CacheKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            Dictionary<string, HashSet<string>> listener = new()
            {
                { articleId, [Context.ConnectionId] }
            };
            return listener;
        }) ?? [];
        listener.TryAdd(articleId, []);
        if (listener[articleId].All(x => x != Context.ConnectionId))
        {
            listener[articleId].Add(Context.ConnectionId);
        }

        return listener[articleId].ToArray();
    }


    private void RemoveListeners()
    {
        var listener = memoryCache.GetOrCreate(CacheKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            Dictionary<string, HashSet<string>> listener = new();
            return listener;
        }) ?? [];
        foreach (var pair in listener)
        {
            if (pair.Value.Any(x => x.Contains(Context.ConnectionId)))
            {
                listener[pair.Key].Remove(Context.ConnectionId);
            }
        }
    }
}