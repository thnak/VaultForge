using BusinessModels.Advertisement;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace Business.SocketHubs;

public class PageCreatorHub(IMemoryCache memoryCache) : Hub
{
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public async Task SendMessage(ArticleModel message)
    {
        memoryCache.Set($"{nameof(ArticleModel)}{message.Title}", message, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
        await Clients.All.SendAsync("ReceiveMessage", message);
    }

    public async Task GetMessages(string articleId)
    {
        var data = memoryCache.Get<ArticleModel>($"{nameof(ArticleModel)}{articleId}");
        if (data == null) await Clients.Caller.SendAsync("ReceiveMessage", articleId);
    }
}