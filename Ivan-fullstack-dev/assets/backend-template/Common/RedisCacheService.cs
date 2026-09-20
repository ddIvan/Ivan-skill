using System.Text.Json;
using Ivan.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IvanProject.Common;

/// <summary>
/// Redis 缓存实现（生产/分布式环境使用），基于 Ivan.Redis 的 RedisClient 静态类。
///
/// 前置条件：
/// 1. csproj 中引入 Ivan.Redis 包；
/// 2. appsettings.json 中配置 Redis 连接：
///    "Redis": {
///      "ConfigKey": "Default",
///      "ConnectionString": "127.0.0.1:6379,password=xxx,defaultDatabase=0"
///    }
///
/// 存储策略：统一序列化为 JSON 字符串存取，
/// 避免 RedisClient 内置序列化器与 .NET 类型兼容性问题。
///
/// 异常安全策略：
///   - 读写 Redis 失败时，Get/GetOrCreate 降级返回 default(T)，不会抛出异常中断请求；
///   - Set/Remove 失败时记录日志，不影响主流程；
///   - 确保 Redis 不可用时系统仍能正常运行（仅缓存失效，功能不中断）。
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly string _configKey;
    private readonly string _prefix = "ivan:cache:";
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromHours(1);

    public RedisCacheService(IConfiguration config, ILogger<RedisCacheService> logger)
    {
        _configKey = config["Redis:ConfigKey"] ?? "Default";
        _logger = logger;

        // 用连接串初始化 RedisClient（WriteServer/ReadServer 均指向同一实例）
        var connStr = config["Redis:ConnectionString"];
        if (!string.IsNullOrEmpty(connStr))
        {
            var redisConfig = new RedisConfig
            {
                WriteServer = connStr,
                ReadServer = connStr
            };
            RedisClient.Init(_configKey, redisConfig);
        }
    }

    // ==================== 读操作（异常安全降级） ====================

    /// <summary>
    /// 从 Redis 获取缓存值。Redis 不可用时返回 default(T)。
    /// </summary>
    public T? Get<T>(string key)
    {
        try
        {
            var json = RedisClient.Get(_configKey, _prefix + key) as string;
            if (string.IsNullOrEmpty(json)) return default;
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis Get 失败，Key={Key}，降级返回 default", _prefix + key);
            return default;
        }
    }

    // ==================== 写操作（异常安全，不影响主流程） ====================

    /// <summary>
    /// 写入缓存。Redis 不可用时仅记录日志，不抛出异常。
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? absoluteExpiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var expireTime = DateTime.Now.Add(absoluteExpiration ?? DefaultExpiration);
            RedisClient.Add(_configKey, _prefix + key, json, expireTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis Set 失败，Key={Key}，缓存写入已跳过", _prefix + key);
        }
    }

    // ==================== 删除操作 ====================

    /// <summary>
    /// 删除缓存键。Redis 不可用时仅记录日志。
    /// </summary>
    public void Remove(string key)
    {
        try
        {
            RedisClient.Remove(_configKey, _prefix + key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis Remove 失败，Key={Key}", _prefix + key);
        }
    }

    // ==================== GetOrCreate（缓存穿透保护） ====================

    /// <summary>
    /// 获取或创建缓存值（双重检查 + 工厂模式）。
    /// 缓存未命中时调用 factory 生成值并写入缓存；
    /// 读写 Redis 失败时降级为直接调用 factory 返回（不缓存，但保证功能可用）。
    /// </summary>
    public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? absoluteExpiration = null)
    {
        // 尝试读取缓存
        try
        {
            var cached = Get<T>(key);
            if (cached != null && !cached.Equals(default(T))) return cached;
        }
        catch
        {
            // Get 内部已降级，此处为额外安全网
        }

        // 缓存未命中，调用工厂生成
        var value = factory();

        // 尝试写入缓存（失败不影响返回值）
        try
        {
            Set(key, value, absoluteExpiration);
        }
        catch
        {
            // Set 内部已降级
        }

        return value;
    }
}
