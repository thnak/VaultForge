using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Business.Services;

public class CacheKeyManager : IDisposable
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

    public void Clear()
    {
        foreach (var pair in _cacheKeys)
        {
            _cache.Remove(pair.Key);
        }

        _cacheKeys.Clear();
    }

    public void RemoveNearestKeys(params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var pair in _cacheKeys.Where(x => x.Key.Contains(key)))
            {
                _cacheKeys.TryRemove(pair.Key, out _);
                _cache.Remove(pair.Key);
            }
        }
    }

    public Task<T?> GetOrCreateAsync<T>(string key, Func<ICacheEntry, Task<T>> factory, MemoryCacheEntryOptions? options = default)
    {
        _cacheKeys.TryAdd(key, false);
        if (options != null)
            options.RegisterPostEvictionCallback(RemoveCallBack);
        return _cache.GetOrCreateAsync($"{_cacheKey}{key}", factory, options);
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

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }
}