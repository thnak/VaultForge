using System.Net;
using BusinessModels.General.Results;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace WebApp.Client.Services.Http;

public partial class BaseHttpClientService
{
    public async Task<ResponseDataResult<string>> UploadFileAsync(string folderAliasCode, HttpContent? content)
    {
        var uri = $"api/files/upload-physical/{folderAliasCode}";
        //Added with .NET9
        var webAssemblyEnableStreamingRequestKey = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingRequest");

        var req = new HttpRequestMessage(HttpMethod.Post, uri);
        req.SetBrowserRequestStreamingEnabled(true);
        
        var antiToken = RequestAntiforgeryStateService.GetAntiforgeryToken()?.Value ?? string.Empty;
        req.Headers.Add("RequestVerificationToken", [antiToken]);
        
//Added with .NET9
        req.Version = HttpVersion.Version20;
//Added with .NET9
        req.Options.Set(webAssemblyEnableStreamingRequestKey, true);

        req.Content = content;

        var response = await PostAsync<string>(req);
        return response;
    }
}