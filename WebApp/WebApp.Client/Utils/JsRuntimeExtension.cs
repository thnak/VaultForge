using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using BusinessModels.Resources;
using Microsoft.JSInterop;

namespace WebApp.Client.Utils;

public static class JsRuntimeExtension
{
    public static async Task<string?> GetCookie(this IJSRuntime jsRuntime, string cookieName)
    {
        var cookieVal = await jsRuntime.InvokeAsync<string?>("getCookie", cookieName);
        return cookieVal;
    }

    public static async Task SetCookie(this IJSRuntime jsRuntime, string cookieName, string cookieValue, int days)
    {
        await jsRuntime.InvokeVoidAsync("setCookie", cookieName, cookieValue, days);
    }

    public static ValueTask CopyToClipBoard(this IJSRuntime jsRuntime, string message)
    {
        return jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", message);
    }

    #region Download

    public static ValueTask Download(this IJSRuntime jsRuntime, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri)
    {
        return jsRuntime.InvokeVoidAsync("Download", requestUri);
    }

    #endregion

    #region Local Storage

    public static async Task SetLocalStorage(this IJSRuntime jsRuntime, string key, string value)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    public static async Task SetLocalStorage(this IJSRuntime jsRuntime, string key, object v)
    {
        var jsonText = JsonSerializer.Serialize(v);
        await jsRuntime.SetLocalStorage(key, jsonText);
    }

    public static async Task<string?> GetLocalStorage(this IJSRuntime jsRuntime, string key)
    {
        return await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }

    public static async Task<T?> GetLocalStorage<T>(this IJSRuntime jsRuntime, string key)
    {
        var textPlan = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(textPlan)) return default;

        return JsonSerializer.Deserialize<T?>(textPlan);
    }

    public static async Task<T> GetLocalStorage<T>(this IJSRuntime jsRuntime, string key, T defaultValue)
    {
        var textPlan = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(textPlan)) return defaultValue;
        try
        {
            return JsonSerializer.Deserialize<T?>(textPlan) ?? defaultValue;
        }
        catch (Exception)
        {
            return defaultValue;
        }
    }

    public static async Task RemoveLocalStorage(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }

    public static async Task ClearLocalStorage(this IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.clear");
    }

    #endregion

    #region Session Storage

    public static async Task SetSessionStorage(this IJSRuntime jsRuntime, string key, string value)
    {
        await jsRuntime.InvokeVoidAsync("sessionStorage.setItem", key, value);
    }

    public static async Task<string?> GetSessionStorage(this IJSRuntime jsRuntime, string key)
    {
        return await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", key);
    }

    public static async Task<T?> GetSessionStorage<T>(this IJSRuntime jsRuntime, string key)
    {
        var textPlan = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", key);
        if (string.IsNullOrEmpty(textPlan)) return default;

        return JsonSerializer.Deserialize<T?>(textPlan);
    }

    public static async Task RemoveSessionStorage(this IJSRuntime jsRuntime, string key)
    {
        await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);
    }

    public static async Task ClearSessionStorage(this IJSRuntime jsRuntime)
    {
        await jsRuntime.InvokeVoidAsync("sessionStorage.clear");
    }

    public static async Task LocationReplace(this IJSRuntime jsRuntime, string uri)
    {
        await jsRuntime.InvokeVoidAsync("window.location.replace", uri);
    }

    #endregion

    #region Culture

    private const string CultureKeyName = "Culture";

    public static async Task SetCulture(this IJSRuntime jsRuntime, string name)
    {
        await jsRuntime.InvokeVoidAsync("setCultureCookie", name, name, CookieNames.Culture, 365);
    }

    public static async Task<string?> GetCulture(this IJSRuntime jsRuntime)
    {
        return await jsRuntime.InvokeAsync<string>("getCultureFromCookie", CookieNames.Culture);
    }

    #endregion

    #region Screen

    public static ValueTask FullScreen(this IJSRuntime self)
    {
        return self.InvokeVoidAsync("RequestFullScreen");
    }

    public static ValueTask ExitFullScreen(this IJSRuntime self)
    {
        return self.InvokeVoidAsync("document.exitFullscreen");
    }

    #endregion

    public static Task SetPreventKey(this IJSRuntime self, params string[] key)
    {
        var listKey = key.ToList();
        return self.SetLocalStorage("PreventKey", listKey);
    }

    public static Task<List<string>> GetPreventKey(this IJSRuntime self)
    {
        return self.GetLocalStorage<List<string>>("PreventKey", []);
    }

    public static ValueTask AddScriptResource(this IJSRuntime self, params string[] resourceName)
    {
        return self.InvokeVoidAsync("AddScriptElement", resourceName);
    }

    public static ValueTask RemoveNode(this IJSRuntime self, string nodeId)
    {
        return self.InvokeVoidAsync("removeNode", nodeId);
    }
}