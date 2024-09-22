using Business.Services;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Ollama;

namespace WebApp.Controllers.Chats;

[ApiController]
[Route("api/[controller]")]
public class ChatWithLlamaController(IMemoryCache memoryCache, ILogger<ChatWithLlamaController> logger, IServiceProvider serviceProvider) : ControllerBase
{
    [HttpPost("chat")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChatLama([FromForm] string? systemPrompt, [FromForm] string question, [FromForm] string model, [FromForm] List<string>? images, [FromForm] bool? autoCallTools, [FromForm] bool? showHistory)
    {
        try
        {
            List<Message> messages = memoryCache.GetOrCreate<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                return [];
            }) ?? [];

            var chat = new ChatWithLlama(systemPrompt ?? string.Empty, new Uri("http://thnakdevserver.ddns.net:11434/api"), serviceProvider, model, autoCallTools is true);
            chat.History = messages.Any() ? [..messages] : chat.History;
            var mess = images != default ? await chat.ChatAsync(question, images, HttpContext.RequestAborted) : await chat.ChatAsync(question, HttpContext.RequestAborted);
            HttpContext.Response.RegisterForDispose(chat);
            
            //messages.Add(mess);
            memoryCache.Set<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, [..chat.History], new MemoryCacheEntryOptions() { Priority = CacheItemPriority.NeverRemove });

            if (showHistory is true)
            {
                var obj = new { Message = mess.Content, Histories = chat.History };
                return Content(obj.ToJson(), MimeTypeNames.Application.Json);
            }

            return Content(mess.Content, MimeTypeNames.Text.RichText);
        }
        catch (OperationCanceledException)
        {
            return Ok("cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            return StatusCode(500, ex.Message);
        }
    }
}
