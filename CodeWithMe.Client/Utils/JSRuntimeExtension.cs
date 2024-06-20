using System.Text.Json;
using Microsoft.JSInterop;

namespace CodeWithMe.Client.Utils;

public static class JSRuntimeExtension
{
    public static async Task<string?> GetCookie(this IJSRuntime jsRuntime, string cookieName)
    {
        string? cookieVal = await jsRuntime.InvokeAsync<string?>("getCookie", cookieName);
        return cookieVal;
    }
    public static async Task SetCookie(this IJSRuntime jsRuntime, string cookieName, string cookieValue, int days)
    {
        await jsRuntime.InvokeVoidAsync("setCookie", cookieName, cookieValue, days);
    }

    #region Local Storage

    public static async Task SetLocalStorage(this IJSRuntime jsRuntime, string key, string value)
    {
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
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

    #endregion
}