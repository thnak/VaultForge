using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Services;

public class CacheKeyManager
{
    private readonly ConcurrentDictionary<string, bool> _cacheKeys = [];
    private readonly Timer _cleanupTimer;
    private readonly IMemoryCache _cache;
    private readonly string _cacheKey;

    public CacheKeyManager(IMemoryCache cache, string prefix)
    {
        _cache = cache;
        _cacheKey = prefix;
        _cleanupTimer = new Timer(CleanupExpiredKeys, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    public void Set(string key, object value, MemoryCacheEntryOptions? options)
    {
        _cacheKeys.TryAdd(key, false);
        if (options != null)
            options.RegisterPostEvictionCallback(RemoveCallBack);
        _cache.Set($"{_cacheKey}{key}", value, options);
    }

    public void Remove(string key)
    {
        _cache.Remove($"{_cacheKey}{key}");
        _cacheKeys.TryRemove(key, out _);
    }

    public Task<T?> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory, MemoryCacheEntryOptions? options = default)
    {
        _cacheKeys.TryAdd(key, false);
        if (options != null)
            options.RegisterPostEvictionCallback(RemoveCallBack);
        return _cache.GetOrCreateAsync($"{_cacheKey}{key}", factory, options);

        return Task.FromResult<T?>(default);
    }

    private void RemoveCallBack(object key, object? value, EvictionReason reason, object? state)
    {
        _cacheKeys.TryRemove(key.ToString()!, out _);
    }

    private void CleanupExpiredKeys(object? state)
    {
        var expiredKeys = _cacheKeys.Where(key => !_cache.TryGetValue(key, out _));
        foreach (var expiredKey in expiredKeys)
        {
            _cache.Remove(expiredKey);
        }
    }
}