using System.Text.Json;
using IvanProject.DTOs;
using IvanProject.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace IvanProject.BLL;

/// <summary>
/// 菜单缓存服务 — 基于 IMemoryCache 缓存菜单树和全量平铺数据。
///
/// 缓存策略：
///   1. 菜单全量树（menu:tree）—— 30分钟过期，供前端侧边栏和权限过滤使用；
///   2. 菜单平铺列表（menu:all）—— 30分钟过期，供管理后台表格展示；
///   3. 单菜单节点（menu:{id}）—— 按需缓存，节点更新时精准清除；
///   4. 所有缓存键以 "menu:" 为前缀，方便统一管理和清除。
///
/// 缓存失效（Cache Invalidation）：
///   - 节点增/删/改/移动/启用禁用 → 全量清除 menu:tree + menu:all + 单节点缓存
///   - RolePermissionService 角色权限变更时也会清除 menu:tree（权限变更影响菜单可见性）
///
/// 异常安全策略：
///   - 所有缓存操作都有 try/catch，失败时降级返回 null（查数据库）；
///   - 不会因为缓存不可用导致菜单功能中断。
///
/// 说明：原实现使用 Ivan.Redis 的静态 RedisClient（其签名与本项目调用方式不符）。
/// 菜单缓存为进程级单例数据，内存缓存即可满足；如需分布式缓存可注入 ICacheService 改造。
/// </summary>
public class MenuCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MenuCacheService> _logger;
    private const string CachePrefix = "menu:";
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan NodeExpiration = TimeSpan.FromMinutes(10);

    public MenuCacheService(IMemoryCache cache, ILogger<MenuCacheService>? logger = null)
    {
        _cache = cache;
        _logger = logger ?? NullLogger.Instance;
    }

    // ==================== 菜单树缓存 ====================

    /// <summary>获取缓存的菜单树（未命中返回 null，触发查库）</summary>
    public List<MenuTreeNode>? GetTree()
    {
        try
        {
            return _cache.TryGetValue<List<MenuTreeNode>>($"{CachePrefix}tree", out var tree) ? tree : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetTree 缓存读取失败，降级返回 null");
            return null;
        }
    }

    /// <summary>设置菜单树缓存（30分钟过期）</summary>
    public void SetTree(List<MenuTreeNode> tree)
    {
        try
        {
            _cache.Set($"{CachePrefix}tree", tree, DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SetTree 缓存写入失败，已跳过");
        }
    }

    // ==================== 全量平铺列表缓存 ====================

    /// <summary>获取缓存的全量菜单平铺列表</summary>
    public List<Menus>? GetAll()
    {
        try
        {
            return _cache.TryGetValue<List<Menus>>($"{CachePrefix}all", out var all) ? all : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetAll 缓存读取失败，降级返回 null");
            return null;
        }
    }

    /// <summary>设置全量菜单平铺列表缓存（30分钟过期）</summary>
    public void SetAll(List<Menus> all)
    {
        try
        {
            _cache.Set($"{CachePrefix}all", all, DefaultExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SetAll 缓存写入失败，已跳过");
        }
    }

    // ==================== 单节点缓存 ====================

    /// <summary>获取缓存的单节点</summary>
    public Menus? GetNode(int id)
    {
        try
        {
            return _cache.TryGetValue<Menus>($"{CachePrefix}{id}", out var node) ? node : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetNode({Id}) 缓存读取失败，降级返回 null", id);
            return null;
        }
    }

    /// <summary>设置单节点缓存（10分钟过期）</summary>
    public void SetNode(Menus node)
    {
        try
        {
            _cache.Set($"{CachePrefix}{node.Id}", node, NodeExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SetNode({Id}) 缓存写入失败，已跳过", node.Id);
        }
    }

    /// <summary>清除单节点缓存</summary>
    public void ClearNode(int id)
    {
        try
        {
            _cache.Remove($"{CachePrefix}{id}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClearNode({Id}) 失败", id);
        }
    }

    // ==================== 全量清除 ====================

    /// <summary>菜单数据变更后全量清除所有菜单缓存</summary>
    public void InvalidateAll()
    {
        try
        {
            _cache.Remove($"{CachePrefix}tree");
            _cache.Remove($"{CachePrefix}all");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InvalidateAll 失败，已跳过缓存清除");
        }
    }

    /// <summary>按模式清除缓存（供 RolePermissionService 调用，内存缓存按前缀清除）</summary>
    public void InvalidateByPattern(string pattern)
    {
        try
        {
            // 内存缓存无法按模式扫描，清除已知的公共键
            _cache.Remove($"{CachePrefix}tree");
            _cache.Remove($"{CachePrefix}all");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "InvalidateByPattern 失败，已跳过缓存清除");
        }
    }
}

/// <summary>
/// 空日志实现（未注入 ILogger 时的 fallback）
/// </summary>
internal class NullLogger : ILogger<MenuCacheService>
{
    public static readonly NullLogger Instance = new();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
