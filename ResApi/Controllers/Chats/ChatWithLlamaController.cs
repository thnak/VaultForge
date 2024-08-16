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
    public async Task<IActionResult> ChatLama([FromForm] string systemPrompt, [FromForm] string question)
    {
        List<Message> messages = memoryCache.GetOrCreate<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            return [];
        }) ?? [];
        
        var chat = new ChatWithLlama(systemPrompt);
        chat.History = messages;
        var mess = await chat.ChatAsync(question, HttpContext.RequestAborted);
        HttpContext.Response.RegisterForDispose(chat);
        return Content(mess.ToJson(), MimeTypeNames.Application.Json);
    }
}