using System.Text;
using Business.Services;
using Business.Services.RetrievalAugmentedGeneration.Interface;
using BusinessModels.General.SettingModels;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ollama;

namespace WebApp.Controllers.Chats;

[ApiController]
[Route("api/[controller]")]
public class ChatWithLlamaController(IMemoryCache memoryCache, ILogger<ChatWithLlamaController> logger, IMovieDatabase movieDatabase, IServiceProvider serviceProvider, IOptions<AppSettings> options) : ControllerBase
{
    [HttpPost("chat")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChatLama([FromForm] string? systemPrompt, [FromForm] string question, [FromForm] string model, [FromForm] List<string>? images, [FromForm] bool? autoCallTools, [FromForm] bool? showHistory, [FromForm] bool? startNew)
    {
        try
        {
            if (startNew is true)
                memoryCache.Remove(nameof(ChatWithLlamaController) + systemPrompt);

            List<Message> messages = memoryCache.GetOrCreate<List<Message>>(nameof(ChatWithLlamaController) + systemPrompt, entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;
                return [];
            }) ?? [];

            var chat = new ChatWithLlama(systemPrompt ?? string.Empty, new Uri($"{options.Value.OllamaConfig.ConnectionString}api"), serviceProvider, model, autoCallTools is true);
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
            memoryCache.Remove(nameof(ChatWithLlamaController) + systemPrompt);
            logger.LogError(ex, ex.Message);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("search-chat")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetEmbeddingAsync([FromForm] string prompt, [FromForm] int maxResult)
    {
        var result = await movieDatabase.SearchAsync(prompt, maxResult);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var searchScore in result.Value)
        {
            stringBuilder.AppendLine($"[{searchScore.Score:N2}] {searchScore.Value.Title}:\nDescription: {searchScore.Value.Description}\n");
        }

        return Ok(stringBuilder.ToString());
    }
}