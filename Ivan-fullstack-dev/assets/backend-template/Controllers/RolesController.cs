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
/// 角色管理接口
/// </summary>
[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRolesBLL _rolesBLL;
    private readonly RolePermissionService _permService;

    public RolesController(IRolesBLL rolesBLL, RolePermissionService permService)
    {
        _rolesBLL = rolesBLL;
        _permService = permService;
    }

    /// <summary>分页查询角色</summary>
    [HttpGet]
    public ApiResult<PageResult<Roles>> GetPage([FromQuery] string? keyword, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var searchModel = new PageSearchModel();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            searchModel["keyword"] = keyword.Trim();
        }
        searchModel.Page = pageIndex < 1 ? 1 : pageIndex;
        searchModel.Limit = pageSize < 1 ? 10 : pageSize;

        var pageResult = _rolesBLL.List(searchModel);
        return ApiResult<PageResult<Roles>>.Ok(new PageResult<Roles>
        {
            Total = pageResult.TotalRecord,
            Items = pageResult.Data ?? new List<Roles>()
        });
    }

    /// <summary>获取所有角色（下拉选择用）</summary>
    [HttpGet("all")]
    public ApiResult<List<Roles>> GetAll()
    {
        return ApiResult<List<Roles>>.Ok(_rolesBLL.GetALL());
    }

    /// <summary>根据 ID 查询角色</summary>
    [HttpGet("{id:int}")]
    public ApiResult<Roles?> GetById(int id)
    {
        return ApiResult<Roles?>.Ok(_rolesBLL.GetById(id));
    }

    /// <summary>新增角色</summary>
    [HttpPost]
    public ApiResult<int> Create(Roles role)
    {
        var result = _rolesBLL.Create(role);
        return result.Success ? ApiResult<int>.Ok(result.Data, "新增成功") : ApiResult<int>.Fail(result.Message ?? "新增失败");
    }

    /// <summary>更新角色</summary>
    [HttpPut("{id:int}")]
    public ApiResult Update(int id, Roles role)
    {
        role.Id = id;
        var result = _rolesBLL.Modify(role);
        return result.Success ? ApiResult.Ok("更新成功") : ApiResult.Fail(result.Message ?? "更新失败");
    }

    /// <summary>删除角色</summary>
    [HttpDelete("{id:int}")]
    public ApiResult Delete(int id)
    {
        var result = _rolesBLL.Remove(id);
        return result.Success ? ApiResult.Ok("删除成功") : ApiResult.Fail(result.Message ?? "删除失败");
    }

    /// <summary>获取角色的菜单ID列表</summary>
    [HttpGet("{id:int}/menus")]
    public ApiResult<List<int>> GetRoleMenus(int id)
    {
        return ApiResult<List<int>>.Ok(_permService.GetRoleMenuIds(id));
    }

    /// <summary>保存角色的菜单权限</summary>
    [HttpPut("{id:int}/menus")]
    public ApiResult SaveRoleMenus(int id, List<int> menuIds)
    {
        _permService.SaveRoleMenus(id, menuIds);
        return ApiResult.Ok("菜单权限保存成功");
    }

    /// <summary>获取角色的按钮权限</summary>
    [HttpGet("{id:int}/buttons")]
    public ApiResult<List<RoleMenuButtons>> GetRoleButtons(int id)
    {
        return ApiResult<List<RoleMenuButtons>>.Ok(_permService.GetRoleButtons(id));
    }

    /// <summary>保存角色的按钮权限</summary>
    [HttpPut("{id:int}/buttons")]
    public ApiResult SaveRoleButtons(int id, RoleButtonPermissionRequest request)
    {
        request.RoleId = id;
        _permService.SaveRoleButtons(id, request.MenuId, request.ButtonKeys);
        return ApiResult.Ok("按钮权限保存成功");
    }
}
