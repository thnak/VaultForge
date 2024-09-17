using Business.Services.OllamaToolCallingServices;
using Business.Services.OllamaToolCallingServices.Interfaces;
using Ollama;

namespace Business.Services;

public class ChatWithLlama : IDisposable
{
    private readonly OllamaApiClient _ollamaApiClient;
    private readonly Chat _chatClient;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="systemPrompt"></param>
    /// <param name="uri">default value is http://localhost:11434/api</param>
    /// <param name="model"></param>
    /// <param name="autoCallTools"></param>
    public ChatWithLlama(string systemPrompt, Uri uri, string model = "llama3.1", bool autoCallTools = false)
    {
        _ollamaApiClient = new OllamaApiClient(null, uri);
        _chatClient = _ollamaApiClient.Chat(
            model: model,
            systemMessage: systemPrompt,
            autoCallTools: autoCallTools);
        if (autoCallTools)
            InitCallService();
    }

    public ChatWithLlama(string systemPrompt, string model = "llama3.1")
    {
        _ollamaApiClient = new OllamaApiClient();
        _chatClient = _ollamaApiClient.Chat(
            model: model,
            systemMessage: systemPrompt,
            autoCallTools: true);

        InitCallService();
    }

    private void InitCallService()
    {
        var service = new MathService();
        var timeService = new TimeService();
        var weatherService = new WeatherService("https://api.weatherapi.com");
        var webCrawler = new LamaWebCrawlerService();

        _chatClient.AddToolService(timeService.AsTools(), timeService.AsCalls());
        _chatClient.AddToolService(service.AsTools(), service.AsCalls());
        _chatClient.AddToolService(weatherService.AsTools(), weatherService.AsCalls());
        _chatClient.AddToolService(webCrawler.AsTools(), webCrawler.AsCalls());
    }

    public Task<Message> ChatAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return _chatClient.SendAsync(prompt, cancellationToken: cancellationToken);
    }

    public Task<Message> ChatAsync(string prompt, List<string> images, CancellationToken cancellationToken = default)
    {
        return _chatClient.SendAsync(prompt, imagesAsBase64: images, cancellationToken: cancellationToken);
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