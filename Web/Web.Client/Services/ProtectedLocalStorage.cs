using System.Text.Json;
using Microsoft.JSInterop;

namespace Web.Client.Services;

public class ProtectedLocalStorage(IJSRuntime jsRuntime)
{
    /// <summary>
    /// Handler function to get key
    /// </summary>
    public Func<Task<string>>? KeyHandler { get; set; }

    private async Task<string> InitializeKeyAsync()
    {

        // Check if a key already exists
        var key = await jsRuntime.InvokeAsync<string>("protectedStorage.getItem", "encryptionKey");
        if (string.IsNullOrEmpty(key))
        {
            // Generate a new key if none exists
            key = await jsRuntime.InvokeAsync<string>("protectedStorage.generateKey");
            await jsRuntime.InvokeVoidAsync("protectedStorage.setItem", "encryptionKey", key);
        }
        return key;
    }

    private Task<string> GetKey()
    {
        if (KeyHandler != null)
        {
            return KeyHandler.Invoke();
        }
        return InitializeKeyAsync();
    }

    public async Task SetAsync(string key, string value)
    {
        var password = await GetKey();
        var result = await jsRuntime.InvokeAsync<Dictionary<string, object>>("protectedStorage.encryptWithPassword", password, value);
        result.TryGetValue("iv", out object? iv);
        result.TryGetValue("data", out object? encryptedData);
        result.TryGetValue("salt", out object? salt);

        if (encryptedData != null) await jsRuntime.InvokeVoidAsync("protectedStorage.setItem", key, encryptedData.ToString());
        if (iv != null) await jsRuntime.InvokeVoidAsync("protectedStorage.setItem", key + "_iv", iv.ToString());
        if (salt != null) await jsRuntime.InvokeVoidAsync("protectedStorage.setItem", key + "_salt", salt.ToString());
    }

    public async Task SetAsync(string key, object value)
    {
        var textPlant = JsonSerializer.Serialize(value);
        await SetAsync(key, textPlant);
    }


    public async Task<string> GetAsync(string key)
    {
        var password = await GetKey();
        var iv = await jsRuntime.InvokeAsync<string>("protectedStorage.getItem", key + "_iv");
        var encryptedData = await jsRuntime.InvokeAsync<string>("protectedStorage.getItem", key);
        var salt = await jsRuntime.InvokeAsync<string>("protectedStorage.getItem", key + "_salt");

        if (string.IsNullOrEmpty(iv) || string.IsNullOrEmpty(encryptedData) || string.IsNullOrEmpty(salt))
        {
            return string.Empty;
        }

        return await jsRuntime.InvokeAsync<string>("protectedStorage.decryptWithPassword", password, iv, encryptedData, salt);
    }

    public async Task<ProtectedBrowserStorageResult<T>> GetAsync<T>(string key)
    {
        var textPlan = await GetAsync(key);
        try
        {
            var re = JsonSerializer.Deserialize<T>(textPlan);
            return new ProtectedBrowserStorageResult<T>(true, re);
        }
        catch (Exception)
        {
            //
        }
        return new ProtectedBrowserStorageResult<T>(false, default);
    }

    public async Task RemoveAsync(string key)
    {
        await jsRuntime.InvokeVoidAsync("protectedStorage.removeItem", key);
        await jsRuntime.InvokeVoidAsync("protectedStorage.removeItem", key + "_iv");
        await jsRuntime.InvokeVoidAsync("protectedStorage.removeItem", key + "_salt");
    }
}

/// <summary>
/// Contains the result of a protected browser storage operation.
/// </summary>
public class ProtectedBrowserStorageResult<TValue>(bool success, TValue? value)
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    /// Gets the result value of the operation.
    /// </summary>
    public TValue? Value { get; } = value;
}