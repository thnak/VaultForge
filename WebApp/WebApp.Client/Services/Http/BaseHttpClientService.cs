using System.Diagnostics.CodeAnalysis;
using BusinessModels.Utils;
using BusinessModels.WebContent;

namespace WebApp.Client.Services.Http;

public class BaseHttpClientService(HttpClient httpClient)
{
    public HttpClient HttpClient = httpClient;

    public string GetBaseUrl()
    {
        return HttpClient.BaseAddress?.ToString() ?? string.Empty;
    }

    public async Task<ResponseData<T>> PostAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content = default, CancellationToken? cancellationToken = default)
    {
        ResponseData<T> responseData = new ResponseData<T>();

        try
        {
            var responseMessage = await httpClient.PostAsync(requestUri, content, cancellationToken ?? default);

            responseData.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
            responseData.StatusCode = responseMessage.StatusCode;
            var responseText = await responseMessage.Content.ReadAsStringAsync();
            if (responseMessage.IsSuccessStatusCode)
            {
                var data = responseText.DeSerialize<T>();
                responseData.Data = data;
            }
            else
            {
                responseData.Message = responseText;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return responseData;
    }

    public async Task<ResponseData<T>> GetAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken? cancellationToken = default)
    {
        ResponseData<T> responseData = new ResponseData<T>();

        try
        {
            var responseMessage = await httpClient.GetAsync(requestUri, cancellationToken ?? default);

            responseData.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
            responseData.StatusCode = responseMessage.StatusCode;
            var responseText = await responseMessage.Content.ReadAsStringAsync();
            if (responseMessage.IsSuccessStatusCode)
            {
                var data = responseText.DeSerialize<T>();
                responseData.Data = data;
            }
            else
            {
                responseData.Message = responseText;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return responseData;
    }
}