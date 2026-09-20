using Ivan.Common;
using IvanProject.BLL;
using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.DTOs;
using IvanProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IvanProject.Controllers;

/// <summary>
/// 用户管理接口（示例：标准 Controller 写法 —— 薄控制器，只做参数处理并调用 BLL）
/// 注意：路由必须显式写 "api/users"，不能用 [controller]——
/// [controller] 解析为控制器类名去 Controller 后缀的小写（UserController → "user" 单数），
/// 与前端约定的复数路径 /api/users 不一致会导致 404
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
[RequirePerm("users:view")]
public class UserController : ControllerBase
{
    private readonly IUsersBLL _usersBLL;
    private readonly AuthService _authService;

    public UserController(IUsersBLL usersBLL, AuthService authService)
    {
        _usersBLL = usersBLL;
        
        _authService = authService;
    }

    /// <summary>分页查询用户（含角色ID列表）</summary>
    [HttpGet]
    public ApiResult<PageResult<object>> GetPage([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var searchModel = new PageSearchModel();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // 约定：必须用 TryGetModelValue 读取，索引访问在 key 不存在时抛 KeyNotFoundException
            searchModel["keyword"] = keyword.Trim();
        }
        searchModel.Page = pageIndex < 1 ? 1 : pageIndex;
        searchModel.Limit = pageSize < 1 ? 10 : pageSize;

        var pageResult = _usersBLL.List(searchModel);
        var items = (pageResult.Data ?? new List<Users>())
            .Select(u => (object)new
            {
                u.Id,
                u.UserName,
                u.DisplayName,
                u.IsEnabled,
                u.CreateTime,
                RoleIds = _usersBLL.GetUserRoleIds(u.Id)
            })
            .ToList();

        return ApiResult<PageResult<object>>.Ok(new PageResult<object>
        {
            Total = pageResult.TotalRecord,
            Items = items
        });
    }

    /// <summary>根据 ID 查询用户（含角色ID列表，编辑弹窗回显用）</summary>
    [HttpGet("{id:int}")]
    public ApiResult<object> GetById(int id)
    {
        var user = _usersBLL.GetById(id);
        if (user == null) return ApiResult<object>.Ok(null);

        return ApiResult<object>.Ok(new
        {
            user.Id,
            user.UserName,
            user.DisplayName,
            user.IsEnabled,
            user.CreateTime,
            RoleIds = _usersBLL.GetUserRoleIds(id)
        });
    }

    /// <summary>新增用户（管理员操作，密码 PBKDF2 哈希存储，支持指定多角色）</summary>
    [RequirePerm("users:add")]
    [HttpPost]
    public ApiResult<int> Create(UserCreateRequest request)
    {
        var userId = _authService.Register(request.UserName.Trim(), request.Password, request.DisplayName);
        if (request.RoleIds is { Count: > 0 })
        {
            _usersBLL.SaveUserRoles(userId, request.RoleIds);
        }
        return ApiResult<int>.Ok(userId, "新增成功");
    }

    /// <summary>更新用户（显示名/启用状态/角色，NewPassword 填写则重置密码）</summary>
    [RequirePerm("users:edit")]
    [HttpPut("{id:int}")]
    public ApiResult Update(int id, UserUpdateRequest request)
    {
        request.Id = id;

        var user = _usersBLL.GetById(id) ?? throw new BusinessException("用户不存在");
        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            user.DisplayName = request.DisplayName;
        }
        if (request.IsEnabled.HasValue)
        {
            user.IsEnabled = request.IsEnabled.Value;
        }
        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = AuthService.HashPassword(request.NewPassword);
        }
        var result = _usersBLL.Modify(user);
        if (!result.Success)
        {
            return ApiResult.Fail(result.Message ?? "更新失败");
        }

        if (request.RoleIds != null)
        {
            _usersBLL.SaveUserRoles(id, request.RoleIds);
        }
        return ApiResult.Ok("更新成功");
    }

    /// <summary>删除用户</summary>
    [RequirePerm("users:delete")]
    [HttpDelete("{id:int}")]
    public ApiResult Delete(int id)
    {
        var result = _usersBLL.Remove(id);
        return result.Success ? ApiResult.Ok("删除成功") : ApiResult.Fail(result.Message ?? "删除失败");
    }
}
