using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace IvanProject.Common;

/// <summary>
/// 可绑定后端接口信息（供菜单按钮-Action 可视化绑定下拉选择）。
/// </summary>
public class ControllerActionInfo
{
    /// <summary>控制器类名（如 UserController）</summary>
    public string Controller { get; set; } = string.Empty;

    /// <summary>Action 方法名（如 Create）</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>HTTP 方法（GET/POST/PUT/DELETE）</summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>完整路由模板（如 api/users/{id:int}）</summary>
    public string Route { get; set; } = string.Empty;

    /// <summary>声明的权限编码（[RequirePerm]，多值用 | 分隔，无注解为空）</summary>
    public string PermCode { get; set; } = string.Empty;

    /// <summary>展示用唯一标识：控制器.方法（与按钮节点 ControllerAction 字段对应）</summary>
    public string FullName => $"{Controller}.{Action}";
}

/// <summary>
/// 后端 Controller Action 反射扫描器：
/// 扫描入口程序集全部控制器中带 HTTP 方法特性的公开 Action，
/// 用于"按钮绑定后端 Action"的可视化配置与合法性校验（结果进程内缓存）。
/// </summary>
public static class ControllerActionScanner
{
    private static List<ControllerActionInfo>? _cache;
    private static readonly object _lock = new();

    /// <summary>获取全部可绑定 Action（按控制器、声明顺序排序）</summary>
    public static List<ControllerActionInfo> GetActions()
    {
        if (_cache != null) return _cache;
        lock (_lock)
        {
            if (_cache != null) return _cache;

            var list = new List<ControllerActionInfo>();
            var entry = Assembly.GetEntryAssembly();
            if (entry == null) return list;

            var controllerTypes = entry.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var type in controllerTypes)
            {
                var controllerRoute = type.GetCustomAttribute<RouteAttribute>()?.Template ?? string.Empty;
                // 与 PermAuthorizationFilter 同优先级：Action 级注解优先于 Controller 级
                var classPerm = type.GetCustomAttribute<RequirePermAttribute>()?.Permissions
                    ?? Array.Empty<string>();

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => !m.IsDefined(typeof(NonActionAttribute))
                             && m.GetCustomAttribute<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>() != null);

                foreach (var m in methods)
                {
                    var httpAttr = m.GetCustomAttribute<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()!;
                    var methodPerm = m.GetCustomAttribute<RequirePermAttribute>();
                    var perms = methodPerm != null ? methodPerm.Permissions : classPerm;

                    list.Add(new ControllerActionInfo
                    {
                        Controller = type.Name,
                        Action = m.Name,
                        HttpMethod = httpAttr.HttpMethods.FirstOrDefault() ?? "GET",
                        Route = CombineRoute(controllerRoute, httpAttr.Template),
                        PermCode = string.Join("|", perms)
                    });
                }
            }

            _cache = list.OrderBy(x => x.Controller, StringComparer.Ordinal)
                         .ThenBy(x => x.Action, StringComparer.Ordinal)
                         .ToList();
            return _cache;
        }
    }

    /// <summary>校验 BindAction（控制器.方法）是否存在</summary>
    public static bool Exists(string bindAction)
    {
        if (string.IsNullOrWhiteSpace(bindAction)) return false;
        return GetActions().Any(x => string.Equals(x.FullName, bindAction, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>拼接控制器路由与 Action 路由模板</summary>
    private static string CombineRoute(string? controllerRoute, string? actionTemplate)
    {
        var parts = new[] { controllerRoute, actionTemplate }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim('/'));
        return string.Join("/", parts);
    }
}
