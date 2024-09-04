using BusinessModels.Advertisement;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace Business.SocketHubs;

public class PageCreatorHub(IMemoryCache memoryCache) : Hub
{
    private CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();

    protected override void Dispose(bool disposing)
    {
        CancellationTokenSource.Cancel();
        base.Dispose(disposing);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        CancellationTokenSource.Cancel();
        return base.OnDisconnectedAsync(exception);
    }

    public override Task OnConnectedAsync()
    {
        CancellationTokenSource = new CancellationTokenSource();
        return base.OnConnectedAsync();
    }


    public async Task SendMessage(ArticleModel message)
    {
        memoryCache.Set($"{nameof(ArticleModel)}{message.Title}", message, new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
        await Clients.All.SendAsync("ReceiveMessage", message, CancellationTokenSource.Token);
    }

    public async Task GetMessages(string articleId)
    {
        var data = memoryCache.Get<ArticleModel>($"{nameof(ArticleModel)}{articleId}");
        if (data == null) await Clients.Caller.SendAsync("ReceiveMessage", articleId, CancellationTokenSource.Token);
    }
}