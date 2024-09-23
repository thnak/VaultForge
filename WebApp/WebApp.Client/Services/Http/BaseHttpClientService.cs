using System.Diagnostics.CodeAnalysis;
using System.Net;
using BusinessModels.Resources;
using BusinessModels.Utils;
using BusinessModels.WebContent;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Services.Http;

public class BaseHttpClientService
{
    public HttpClient? HttpClient { get; set; }
    public NavigationManager? Navigation { get; set; }
    public BaseHttpClientService()
    {
    }

    public BaseHttpClientService(HttpClient httpClient, IServiceProvider serviceProvider)
    {
        HttpClient = httpClient;
        using var scoped = serviceProvider.CreateScope();
        Navigation = scoped.ServiceProvider.GetService<NavigationManager>()!;
    }

    public void InitServices(HttpClient httpClient, NavigationManager navigation)
    {
        HttpClient = httpClient;
        Navigation = navigation;
    }

    public string GetBaseUrl()
    {
        return HttpClient?.BaseAddress?.ToString() ?? string.Empty;
    }

    public async Task<ResponseData<T>> PostAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content = default, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseData<T>();

        try
        {
            if (HttpClient != null)
            {
                var responseMessage = await HttpClient.PostAsync(requestUri, content, cancellationToken);
                if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                    if (responseMessage.Headers.Location != null)
                    {
                        Navigation?.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
                    }

                responseData.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
                responseData.StatusCode = responseMessage.StatusCode;

                var responseText = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
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
        }
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_PostAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }

    public async Task<ResponseData<T>> PutAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content = default, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseData<T>();

        try
        {
            if (HttpClient != null)
            {
                var responseMessage = await HttpClient.PutAsync(requestUri, content, cancellationToken);
                if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                    if (responseMessage.Headers.Location != null)
                    {
                        if (Navigation != null) Navigation.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
                    }

                responseData.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
                responseData.StatusCode = responseMessage.StatusCode;
                var responseText = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
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
        }
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_PutAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }

    public async Task<ResponseData<T>> GetAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseData<T>();

        try
        {
            if (HttpClient != null)
            {
                var responseMessage = await HttpClient.GetAsync(requestUri, cancellationToken);
                if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                    if (responseMessage.Headers.Location != null)
                    {
                        if (Navigation != null) Navigation.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
                    }

                responseData.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
                responseData.StatusCode = responseMessage.StatusCode;
                var responseText = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
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
        }
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_GetAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }

    public async Task<ResponseData<T>> DeleteAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseData<T>();

        try
        {
            if (HttpClient != null)
            {
                var responseMessage = await HttpClient.DeleteAsync(requestUri, cancellationToken);
                if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                    if (responseMessage.Headers.Location != null)
                    {
                        Navigation?.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
                    }

                responseData.IsSuccessStatusCode = responseMessage.IsSuccessStatusCode;
                responseData.StatusCode = responseMessage.StatusCode;
                var responseText = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
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
        }
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_DeleteAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }
}