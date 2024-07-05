namespace Web.Client.Services.Http;

public class BaseHttpClientService(HttpClient httpClient)
{
    public HttpClient HttpClient = httpClient;
    public string GetBaseUrl()
    {
        return HttpClient.BaseAddress?.ToString() ?? string.Empty;
    }
}