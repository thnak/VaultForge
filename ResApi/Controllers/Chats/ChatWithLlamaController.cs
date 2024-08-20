using Business.Services;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Ollama;

namespace ResApi.Controllers.Chats;

[ApiController]
[Route("api/[controller]")]
public class ChatWithLlamaController(IMemoryCache memoryCache) : ControllerBase
{
    [HttpPost("chat")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChatLama([FromForm] string systemPrompt, [FromForm] string question, [FromForm] string model, [FromForm] List<string>? images)
    {
        List<Message> messages = memoryCache.GetOrCreate<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return [];
        }) ?? [];

        var chat = new ChatWithLlama(systemPrompt, new Uri("http://192.168.1.18:11434/api"), model);
        chat.History = messages.Any() ? [..messages] : chat.History;
        var mess = images != default ? await chat.ChatAsync(question, images, HttpContext.RequestAborted) : await chat.ChatAsync(question, HttpContext.RequestAborted);
        HttpContext.Response.RegisterForDispose(chat);

        memoryCache.Set<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, [..chat.History], new MemoryCacheEntryOptions() { Priority = CacheItemPriority.NeverRemove });

        var obj = new { Message = mess.Content, Histories = chat.History };
        return Content(obj.ToJson(), MimeTypeNames.Application.Json);
    }
}