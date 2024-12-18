using System.Text.Json;
using Microsoft.JSInterop;
using WebApp.Client.Models;

namespace WebApp.Client.Services.UserInterfaces;

public class ProtectedLocalStorage(IJSRuntime jsRuntime)
{
    /// <summary>
    ///     Handler function to get key
    /// </summary>
    public Func<Task<string>>? KeyHandler { get; set; }

    private async Task<string> InitializeKeyAsync()
    {
        // Check if a key already exists
        var key = await jsRuntime.InvokeAsync<string>("protectedLocalStorage.getItem", "encryptionKey");
        if (string.IsNullOrEmpty(key))
        {
            // Generate a new key if none exists
            key = await jsRuntime.InvokeAsync<string>("protectedLocalStorage.generateKey");
            await jsRuntime.InvokeVoidAsync("protectedLocalStorage.setItem", "encryptionKey", key);
        }

        return key;
    }

    private Task<string> GetKey()
    {
        if (KeyHandler != null) return KeyHandler.Invoke();
        return InitializeKeyAsync();
    }

    public async Task SetAsync(string key, string value)
    {
        var password = await GetKey();
        var result = await jsRuntime.InvokeAsync<Dictionary<string, object>>("protectedLocalStorage.encryptWithPassword", password, value);
        result.TryGetValue("iv", out var iv);
        result.TryGetValue("data", out var encryptedData);
        result.TryGetValue("salt", out var salt);

        if (encryptedData != null) await jsRuntime.InvokeVoidAsync("protectedLocalStorage.setItem", key, encryptedData.ToString());
        if (iv != null) await jsRuntime.InvokeVoidAsync("protectedLocalStorage.setItem", key + "_iv", iv.ToString());
        if (salt != null) await jsRuntime.InvokeVoidAsync("protectedLocalStorage.setItem", key + "_salt", salt.ToString());
    }

    public async Task SetAsync(string key, object value)
    {
        var textPlant = JsonSerializer.Serialize(value);
        await SetAsync(key, textPlant);
    }


    public async Task<string> GetAsync(string key)
    {
        var password = await GetKey();
        var iv = await jsRuntime.InvokeAsync<string>("protectedLocalStorage.getItem", key + "_iv");
        var encryptedData = await jsRuntime.InvokeAsync<string>("protectedLocalStorage.getItem", key);
        var salt = await jsRuntime.InvokeAsync<string>("protectedLocalStorage.getItem", key + "_salt");

        if (string.IsNullOrEmpty(iv) || string.IsNullOrEmpty(encryptedData) || string.IsNullOrEmpty(salt)) return string.Empty;

        return await jsRuntime.InvokeAsync<string>("protectedLocalStorage.decryptWithPassword", password, iv, encryptedData, salt);
    }

    public async Task<ProtectedBrowserStorageResult<T>> GetAsync<T>(string key)
    {
        var textPlan = await GetAsync(key);

        var re = JsonSerializer.Deserialize<T>(textPlan);
        return new ProtectedBrowserStorageResult<T>(true, re);
    }

    public async Task RemoveAsync(string key)
    {
        await jsRuntime.InvokeVoidAsync("protectedLocalStorage.removeItem", key);
        await jsRuntime.InvokeVoidAsync("protectedLocalStorage.removeItem", key + "_iv");
        await jsRuntime.InvokeVoidAsync("protectedLocalStorage.removeItem", key + "_salt");
    }
}