using Business.Services;
using Business.Services.Ffmpeg;
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

        var chat = new ChatWithLlama(systemPrompt, new Uri("http://192.168.1.18:11434/api"), model, autoCallTools is true);
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

    [HttpPost("handle-file")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleFileSystem([FromForm] string filePath)
    {
        string systemPrompt = $"you are auto bot and apart of media server service. your work is generate command and then use ExecuteCommand to run your command prompt to convert video {filePath} to HLS files to help server serve it to user via FFMPEG. if video is 4K you have" +
                              $"to convert it to 4k, 2k, 1080 and 480p version that help user can choose the best one to play with their limit on network. videos must be have h264 format for browsing compatibility. dont try to chat with user because you are in work alone and stuck with command prompt.";

        var result = TerminalExtension.ExecuteCommand($"ffprobe -v quiet -print_format json -show_format -show_streams \"{filePath}\"");


        var chat = new ChatWithLlama(systemPrompt, new Uri("http://192.168.1.18:11434/api"), "llama3.1", true);
        while (true)
        {
            var message = await chat.ChatAsync($"{result}. you have to finish your work by your self");
            if (message.Content == "BREAK") break;
        }


        return Ok();
    }
}