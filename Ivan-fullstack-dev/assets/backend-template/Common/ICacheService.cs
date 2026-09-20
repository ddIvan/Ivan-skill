namespace IvanProject.Common;

/// <summary>
/// 缓存服务抽象——对上层屏蔽 Redis/MemoryCache 差异。
/// 默认实现：<see cref="MemoryCacheService"/>（零依赖，开箱即用）；
/// 生产环境切换到 <see cref="RedisCacheService"/> 需引入 Ivan.Redis + StackExchange.Redis。
/// </summary>
public interface ICacheService
{
    /// <summary>获取缓存项</summary>
    T? Get<T>(string key);

    /// <summary>设置缓存项（支持绝对过期）</summary>
    void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null);

    /// <summary>移除缓存项</summary>
    void Remove(string key);

    /// <summary>获取或创建缓存项（线程安全）</summary>
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null);
}
