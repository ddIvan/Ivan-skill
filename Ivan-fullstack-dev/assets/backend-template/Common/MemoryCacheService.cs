using Microsoft.Extensions.Caching.Memory;

namespace IvanProject.Common;

/// <summary>
/// 内存缓存实现（开发/单机环境使用）。
/// 生产环境分布式部署应切换到 <see cref="RedisCacheService"/>。
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public T? Get<T>(string key)
    {
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (absoluteExpiration.HasValue)
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        _cache.Set(key, value, options);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null)
    {
        return _cache.GetOrCreate(key, entry =>
        {
            if (absoluteExpiration.HasValue)
                entry.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            return factory();
        })!;
    }
}
