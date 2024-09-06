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
    public async Task<IActionResult> ChatLama([FromForm] string systemPrompt, [FromForm] string question, [FromForm] string model, [FromForm] List<string>? images, [FromForm] bool? autoCallTools, [FromForm] bool? showHistory)
    {
        List<Message> messages = memoryCache.GetOrCreate<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return [];
        }) ?? [];

        var chat = new ChatWithLlama(systemPrompt, new Uri("http://localhost:11434/api"), model, autoCallTools is true);
        chat.History = messages.Any() ? [..messages] : chat.History;
        var mess = images != default ? await chat.ChatAsync(question, images, HttpContext.RequestAborted) : await chat.ChatAsync(question, HttpContext.RequestAborted);
        HttpContext.Response.RegisterForDispose(chat);

        memoryCache.Set<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, [..chat.History], new MemoryCacheEntryOptions() { Priority = CacheItemPriority.NeverRemove });

        if (showHistory is true)
        {
            var obj = new { Message = mess.Content, Histories = chat.History };
            return Content(obj.ToJson(), MimeTypeNames.Application.Json);
        }

        return Content(mess.Content, MimeTypeNames.Text.RichText);
    }
}