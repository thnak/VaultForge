using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public HttpClient HttpClient { get; set; }
    public NavigationManager Navigation { get; set; }


    public BaseHttpClientService(NavigationManager navigation)
    {
        Navigation = navigation;
        var httpClient = new HttpClient(new CookieHandler());
        httpClient.BaseAddress = new Uri(Navigation.BaseUri);
        HttpClient = httpClient;
    }

    public string GetBaseUrl()
    {
        return HttpClient.BaseAddress?.ToString() ?? string.Empty;
    }

    public async Task<ResponseDataResult<T>> PostAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content = null, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseDataResult<T>();

        try
        {
            var responseMessage = await HttpClient.PostAsync(requestUri, content, cancellationToken);
            if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                if (responseMessage.Headers.Location != null)
                {
                    Navigation.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
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
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_PostAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }

    public async Task<ResponseDataResult<T>> PutAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content = null, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseDataResult<T>();

        try
        {
            var responseMessage = await HttpClient.PutAsync(requestUri, content, cancellationToken);
            if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                if (responseMessage.Headers.Location != null)
                {
                    Navigation.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
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
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_PutAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }

    public async Task<ResponseDataResult<T>> GetAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseDataResult<T>();

        try
        {
            var responseMessage = await HttpClient.GetAsync(requestUri, cancellationToken);
            if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                if (responseMessage.Headers.Location != null)
                {
                    Navigation.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
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
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_GetAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }

    public async Task<ResponseDataResult<T>> DeleteAsync<T>([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseDataResult<T>();

        try
        {
            var responseMessage = await HttpClient.DeleteAsync(requestUri, cancellationToken);
            if (responseMessage is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently })
                if (responseMessage.Headers.Location != null)
                {
                    Navigation.NavigateTo(responseMessage.Headers.Location.ToString(), forceRedirect);
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
        catch (Exception e)
        {
            Console.WriteLine(AppLang.BaseHttpClientService_DeleteAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }

    public async Task RequestCulture()
    {
        var uri = Navigation.GetUriWithQueryParameters(Navigation.BaseUri + $"culture/get", new Dictionary<string, object?>() { { "culture", CultureInfo.CurrentCulture.Name } });
        await HttpClient.GetAsync(uri);
    }
}