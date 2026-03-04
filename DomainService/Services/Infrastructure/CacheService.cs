using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DomainService.ServiceContract;

namespace DomainService.Services.Infrastructure;

public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly object _cleanupLock = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        if (_cache.TryGetValue(key, out var cacheItem))
        {
            // 检查是否过期
            if (cacheItem.ExpirationTime > DateTime.UtcNow)
            {
                return Task.FromResult((T?)cacheItem.Value);
            }
            else
            {
                // 过期则移除
                _cache.TryRemove(key, out _);
            }
        }

        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var expirationTime = DateTime.UtcNow.Add(expiration);
        var cacheItem = new CacheItem
        {
            Value = value,
            ExpirationTime = expirationTime,
            CreatedTime = DateTime.UtcNow
        };

        _cache[key] = cacheItem;

        // 触发清理过期项（简单实现）
        CleanupExpiredItems();

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("缓存键不能为空", nameof(key));

        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private void CleanupExpiredItems()
    {
        // 防止频繁清理，可以添加时间间隔检查
        lock (_cleanupLock)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpirationTime <= now)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private class CacheItem
    {
        public object Value { get; set; } = null!;
        public DateTime ExpirationTime { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}