using Business.Services.OllamaToolCallingServices;
using Business.Services.OllamaToolCallingServices.Interfaces;
using Ollama;

namespace Business.Services;

public class ChatWithLlama : IDisposable
{
    private readonly OllamaApiClient _ollamaApiClient;
    private readonly Chat _chatClient;
    
    public ChatWithLlama(string systemPrompt)
    {
        _ollamaApiClient = new OllamaApiClient();
        _chatClient = _ollamaApiClient.Chat(
            model: "llama3.1",
            systemMessage: systemPrompt,
            autoCallTools: true);

        var service = new MathService();
        _chatClient.AddToolService(service.AsTools(), service.AsCalls());
    }

    public Task<Message> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return _chatClient.SendAsync(prompt, cancellationToken: cancellationToken);
    }

    public List<Message> History
    {
        get => _chatClient.History;
        set => _chatClient.History = value;
    }

    public void Dispose()
    {
        _ollamaApiClient.Dispose();
    }
}