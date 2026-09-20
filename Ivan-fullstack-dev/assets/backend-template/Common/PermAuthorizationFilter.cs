using System.Security.Claims;
using IvanProject.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IvanProject.Common;

/// <summary>
/// 权限授权过滤器——配合 [RequirePerm] 注解使用。
/// 
/// 工作流程：
/// 1. 从 Action / Controller 上获取 [RequirePerm] 注解；
/// 2. 若没有注解，跳过校验；
/// 3. 从 JWT Claims 中获取当前用户的所有角色编码；
/// 4. 通过 <see cref="RolePermissionService"/> 检查用户是否拥有声明权限中的任一权限；
/// 5. 通过则放行，不通过则返回 403。
/// </summary>
public class PermAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly RolePermissionService _permService;

    public PermAuthorizationFilter(RolePermissionService permService)
    {
        _permService = permService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 若已有 [Authorize] 且未认证，由框架中间件返回 401，此处不做额外处理
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            return;

        // 获取 [RequirePerm]：优先 Action 级注解（更具体，如 users:delete），其次 Controller 级（如 users:view）。
        // 注意不能用 EndpointMetadata.OfType().FirstOrDefault()——MVC 元数据中 Controller 特性排在 Action
        // 特性之前，FirstOrDefault 永远取到类级注解，导致 action 级权限（add/edit/delete）全部失效
        RequirePermAttribute? permAttr = null;
        if (context.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor cad)
        {
            permAttr = cad.MethodInfo.GetCustomAttributes(typeof(RequirePermAttribute), true)
                .OfType<RequirePermAttribute>().FirstOrDefault()
                ?? cad.ControllerTypeInfo.GetCustomAttributes(typeof(RequirePermAttribute), true)
                    .OfType<RequirePermAttribute>().FirstOrDefault();
        }
        else
        {
            permAttr = context.ActionDescriptor.EndpointMetadata.OfType<RequirePermAttribute>().FirstOrDefault();
        }

        // 没有声明权限注解，跳过校验
        if (permAttr == null)
            return;

        // 获取当前用户的所有角色
        var roles = context.HttpContext.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        // admin 角色拥有所有权限，直接放行
        if (roles.Contains("admin"))
            return;

        if (roles.Count == 0)
        {
            // 返回统一 ApiResult 格式，前端拦截器可正确解析
            context.Result = new ObjectResult(ApiResult.Fail("权限不足：未分配角色", 403)) { StatusCode = 200 };
            return;
        }

        // 检查用户是否拥有任一所需权限
        foreach (var roleCode in roles)
        {
            if (await _permService.HasAnyPermissionAsync(roleCode, permAttr.Permissions))
            {
                return; // 拥有至少一个权限，放行
            }
        }

        // 返回统一 ApiResult 格式，前端拦截器可正确解析
        context.Result = new ObjectResult(ApiResult.Fail($"权限不足：缺少 {string.Join(" 或 ", permAttr.Permissions)} 权限", 403)) { StatusCode = 200 };
    }
}
