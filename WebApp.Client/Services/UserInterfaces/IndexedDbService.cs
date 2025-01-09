using BusinessModels.Attribute;
using BusinessModels.General.Results;
using BusinessModels.Resources;
using Microsoft.JSInterop;

namespace WebApp.Client.Services.UserInterfaces;

public interface IIndexedDbService<T> : IDisposable where T : class
{
    Task<Result<bool>> CreateStoreAsync(string dbName, string storeName, int version, Func<int, int, Task>? upgradeCallback = null);
    Task<Result<bool>> AddItemAsync(string dbName, string storeName, T item);
    Task<Result<bool>> AddFileAsync(string dbName, string storeName, Stream stream, string fileName, string contentType = "application/octet-stream");
    Task<Result<T?>> GetItemAsync(string dbName, string storeName, string id);
    Task<Result<bool>> DeleteItemAsync(string dbName, string storeName, string id);
}

public class IndexedDbService<T> : UpgradeCallbackHandler, IIndexedDbService<T> where T : class
{
    private readonly IJSRuntime _jsRuntime;
    private readonly string _keyPath;
    private DotNetObjectReference<IndexedDbService<T>>? _dotNetRef;

    public IndexedDbService(IJSRuntime jsRuntime, ILogger<IIndexedDbService<T>> logger) : base(logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

        // Use reflection to find the property with IndexedDbKey attribute
        var keyProperty = typeof(T).GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(IndexedDbKeyAttribute)));

        if (keyProperty == null)
        {
            throw new InvalidOperationException($"No property in {typeof(T).Name} is marked with [IndexedDbKey].");
        }

        _keyPath = char.ToLower(keyProperty.Name[0]) + keyProperty.Name.Substring(1);
    }

    public async Task<Result<bool>> CreateStoreAsync(string dbName, string storeName, int version, Func<int, int, Task>? upgradeCallback = null)
    {
        try
        {
            Callback += upgradeCallback;
            _dotNetRef = DotNetObjectReference.Create(this);
            // Initialize the IndexedDB store (you'll need to pass the upgrade handler to the JS runtime)
            await _jsRuntime.InvokeVoidAsync("indexedDbHelper.createStore", dbName, storeName, version, _keyPath, _dotNetRef);
        
            return Result<bool>.Success(true);
        }
        catch (JSException ex)
        {
            return Result<bool>.Failure($"Failed to create store: {ex.Message}", ErrorType.JavaScriptError);
        }
    }

    public async Task<Result<bool>> AddItemAsync(string dbName, string storeName, T item)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("indexedDbHelper.addItem", dbName, storeName, item);
            return Result<bool>.Success(true);
        }
        catch (JSException ex)
        {
            return Result<bool>.Failure($"Failed to add item: {ex.Message}", ErrorType.JavaScriptError);
        }
    }

    public async Task<Result<bool>> AddFileAsync(string dbName, string storeName, Stream stream, string fileName, string contentType = "application/octet-stream")
    {
        try
        {
            var strRef = new DotNetStreamReference(stream);
            await _jsRuntime.InvokeVoidAsync("indexedDbHelper.addFile", dbName, storeName, strRef, contentType, fileName);
            return Result<bool>.Success(true);
        }
        catch (JSException ex)
        {
            return Result<bool>.Failure($"Failed to add item: {ex.Message}", ErrorType.JavaScriptError);
        }    }


    public async Task<Result<T?>> GetItemAsync(string dbName, string storeName, string id)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<T>("indexedDbHelper.getItem", dbName, storeName, id);
            return Result<T?>.Success(result);
        }
        catch (JSException ex)
        {
            return Result<T?>.Failure($"Failed to get item: {ex.Message}", ErrorType.JavaScriptError);
        }
    }

    public async Task<Result<bool>> DeleteItemAsync(string dbName, string storeName, string id)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("indexedDbHelper.deleteItem", dbName, storeName, id);
            return Result<bool>.SuccessWithMessage(true, AppLang.Delete_successfully);
        }
        catch (JSException ex)
        {
            return Result<bool>.Failure($"Failed to get item: {ex.Message}", ErrorType.JavaScriptError);
        }
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        Callback = null;
    }
}

public class UpgradeCallbackHandler(ILogger logger)
{
    public Func<int, int, Task>? Callback;

    // Constructor accepts the callback function

    // This method is called by JavaScript during IndexedDB version upgrade
    [JSInvokable("UpgradeCallback")]
    public async Task Invoke(int oldVersion, int newVersion)
    {
        try
        {
            // Ensure the callback is not null before calling
            if (Callback != null)
            {
                // Call the callback with the old and new versions
                await Callback(oldVersion, newVersion);
            }
        }
        catch (Exception ex)
        {
            // Handle any errors that occur during the callback
            logger.LogError($"Error in UpgradeCallbackHandler.Invoke: {ex.Message}");
        }
    }
}

public class IndexedDbResult
{
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? DbName { get; set; }
}