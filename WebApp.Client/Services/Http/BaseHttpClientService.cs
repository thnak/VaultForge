using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Blazored.Toast.Services;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public HttpClient HttpClient { get; }
    private NavigationManager Navigation { get; }
    private IDialogService DialogService { get; set; }
    private  IToastService ToastService { get; set; }
    private ILogger<BaseHttpClientService> Logger { get; set; }
    private AuthenticationStateProvider PersistentAuthenticationStateService { get; set; }
    public BaseHttpClientService(NavigationManager navigation, IDialogService dialogService, IToastService toastService, ILogger<BaseHttpClientService> logger, AuthenticationStateProvider persistentAuthenticationStateService)
    {
        Navigation = navigation;
        var httpClient = new HttpClient(new CookieHandler());
        httpClient.BaseAddress = new Uri(Navigation.BaseUri);
        HttpClient = httpClient;
        DialogService = dialogService;
        ToastService = toastService;
        Logger = logger;
        PersistentAuthenticationStateService = persistentAuthenticationStateService;
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
            Logger.LogError(AppLang.BaseHttpClientService_PostAsync__ERROR___0_, e.Message);
        }

        return responseData;
    }
    
    public async Task<ResponseDataResult<string>> PostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, HttpContent? content = null, CancellationToken cancellationToken = default, bool forceRedirect = true)
    {
        var responseData = new ResponseDataResult<string>();

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
            responseData.Message = responseText;
            responseData.Data = responseText;
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
        var uri = Navigation.GetUriWithQueryParameters(Navigation.BaseUri + $"culture/set", new Dictionary<string, object?>() { { "culture", CultureInfo.CurrentCulture.Name } });
        await HttpClient.GetAsync(uri);
    }
}