using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.DTOs;
using IvanProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IvanProject.Controllers;

/// <summary>
/// 菜单管理接口 — 支持无限级嵌套菜单的增删改查、排序拖拽、启用禁用、懒加载
/// </summary>
[ApiController]
[Route("api/menus")]
[Authorize]
[RequirePerm("menus:view")]
public class MenusController : ControllerBase
{
    private readonly IMenusBLL _menusBLL;

    public MenusController(IMenusBLL menusBLL)
    {
        _menusBLL = menusBLL;
    }

    // ==================== 查询 ====================

    /// <summary>获取菜单树（全量，含递归 children）</summary>
    [HttpGet("tree")]
    public ApiResult<List<Menus>> GetTree()
    {
        return ApiResult<List<Menus>>.Ok(_menusBLL.GetTree());
    }

    /// <summary>
    /// 获取前端侧边栏菜单树（DTO，仅返回启用+可见节点，含 HasChildren 懒加载标识）
    /// </summary>
    [AllowAnonymous] // 登录后调用，不校验具体权限
    [HttpGet("frontend-tree")]
    public ApiResult<List<MenuTreeNode>> GetFrontendTree()
    {
        return ApiResult<List<MenuTreeNode>>.Ok(_menusBLL.GetTreeForFrontend());
    }

    /// <summary>
    /// 懒加载：获取指定节点的直接子节点。
    /// 前端展开节点时按需调用，避免一次性传输整棵大树。
    /// </summary>
    [HttpGet("{parentId:int}/children")]
    public ApiResult<List<MenuTreeNode>> GetChildren(int parentId)
    {
        return ApiResult<List<MenuTreeNode>>.Ok(_menusBLL.GetChildren(parentId));
    }

    /// <summary>获取所有菜单（平铺列表，按 Level + Sort 排序）</summary>
    [HttpGet]
    public ApiResult<List<Menus>> GetAll()
    {
        return ApiResult<List<Menus>>.Ok(_menusBLL.GetAll());
    }

    /// <summary>
    /// 获取可绑定的后端接口列表（反射扫描全部 Controller 的 Http Action），
    /// 供菜单管理页"按钮节点 → 绑定接口"下拉选择，用于前后端权限一致性核对。
    /// </summary>
    [HttpGet("bindable-actions")]
    public ApiResult<List<ControllerActionInfo>> GetBindableActions()
    {
        return ApiResult<List<ControllerActionInfo>>.Ok(ControllerActionScanner.GetActions());
    }

    /// <summary>根据 ID 查询菜单</summary>
    [HttpGet("{id:int}")]
    public ApiResult<Menus?> GetById(int id)
    {
        var menu = _menusBLL.GetById(id);
        return menu != null ? ApiResult<Menus?>.Ok(menu) : ApiResult<Menus?>.Fail("菜单不存在");
    }

    /// <summary>按路由路径查询菜单</summary>
    [HttpGet("by-path")]
    public ApiResult<Menus?> GetByPath([FromQuery] string path)
    {
        var menu = _menusBLL.GetByPath(path);
        return menu != null ? ApiResult<Menus?>.Ok(menu) : ApiResult<Menus?>.Fail("路径不存在");
    }

    /// <summary>按 FullPath 查询菜单</summary>
    [HttpGet("by-fullpath")]
    public ApiResult<Menus?> GetByFullPath([FromQuery] string fullPath)
    {
        var menu = _menusBLL.GetByFullPath(fullPath);
        return menu != null ? ApiResult<Menus?>.Ok(menu) : ApiResult<Menus?>.Fail("路径不存在");
    }

    /// <summary>获取指定节点的所有子孙节点（含自身）</summary>
    [HttpGet("{id:int}/descendants")]
    public ApiResult<List<Menus>> GetDescendants(int id)
    {
        return ApiResult<List<Menus>>.Ok(_menusBLL.GetDescendants(id));
    }

    // ==================== 新增 ====================

    /// <summary>新增菜单节点（自动计算 FullPath、Level、Sort）</summary>
    [RequirePerm("menus:add")]
    [HttpPost]
    public ApiResult<int> Create([FromBody] MenuCreateRequest request)
    {
        var model = new Menus
        {
            Name = request.Name,
            MenuType = request.MenuType,
            Path = request.Path,
            Icon = request.Icon,
            ParentId = request.ParentId,
            Sort = request.Sort ?? 0,
            IsVisible = request.IsVisible,
            IsEnabled = request.IsEnabled,
            PermissionCode = request.PermissionCode,
            ControllerAction = request.ControllerAction,
            Remark = request.Remark
        };

        var result = _menusBLL.Create(model);
        return result.Success ? ApiResult<int>.Ok(result.Data, "新增成功") : ApiResult<int>.Fail(result.Message ?? "新增失败");
    }

    // ==================== 更新 ====================

    /// <summary>更新菜单基本信息</summary>
    [RequirePerm("menus:edit")]
    [HttpPut("{id:int}")]
    public ApiResult Update(int id, [FromBody] MenuUpdateRequest request)
    {
        var result = _menusBLL.Modify(new Menus
        {
            Id = id,
            Name = request.Name,
            MenuType = request.MenuType,
            Path = request.Path,
            Icon = request.Icon,
            ParentId = request.ParentId,
            Sort = request.Sort,
            IsVisible = request.IsVisible,
            IsEnabled = request.IsEnabled,
            PermissionCode = request.PermissionCode,
            ControllerAction = request.ControllerAction,
            Remark = request.Remark
        });
        return result.Success ? ApiResult.Ok("更新成功") : ApiResult.Fail(result.Message ?? "更新失败");
    }

    /// <summary>启用/禁用菜单节点</summary>
    [RequirePerm("menus:edit")]
    [HttpPut("{id:int}/enabled")]
    public ApiResult SetEnabled(int id, [FromQuery] bool enabled)
    {
        var result = _menusBLL.SetEnabled(id, enabled);
        return result.Success ? ApiResult.Ok(enabled ? "已启用" : "已禁用") : ApiResult.Fail(result.Message ?? "操作失败");
    }

    // ==================== 排序/拖拽 ====================

    /// <summary>
    /// 拖拽排序：移动节点到指定位置。
    /// 支持同级内排序和跨父级移动，自动级联更新 FullPath。
    /// </summary>
    [RequirePerm("menus:edit")]
    [HttpPut("reorder")]
    public ApiResult Reorder([FromBody] MenuSortRequest request)
    {
        var result = _menusBLL.Reorder(request);
        return result.Success ? ApiResult.Ok("排序已更新") : ApiResult.Fail(result.Message ?? "排序失败");
    }

    // ==================== 删除 ====================

    /// <summary>删除菜单节点（含所有子孙节点，同时清除关联角色数据）</summary>
    [RequirePerm("menus:delete")]
    [HttpDelete("{id:int}")]
    public ApiResult Delete(int id)
    {
        var result = _menusBLL.Remove(id);
        return result.Success ? ApiResult.Ok("删除成功") : ApiResult.Fail(result.Message ?? "删除失败");
    }
}
