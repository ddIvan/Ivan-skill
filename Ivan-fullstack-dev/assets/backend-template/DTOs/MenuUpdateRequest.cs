using System.ComponentModel.DataAnnotations;

namespace IvanProject.DTOs;

/// <summary>菜单更新请求</summary>
public class MenuUpdateRequest
{
    /// <summary>菜单名称</summary>
    [Required(ErrorMessage = "菜单名称不能为空")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>节点类型：1=目录 2=菜单(页面) 3=按钮</summary>
    [Range(1, 3, ErrorMessage = "MenuType 必须为 1/2/3")]
    public int MenuType { get; set; } = 2;

    /// <summary>路由路径（菜单节点必填，目录/按钮节点可为空）</summary>
    [MaxLength(200)]
    public string? Path { get; set; }

    /// <summary>图标</summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>父菜单ID（null/0=根节点；更新时不允许改变，仅拖拽/Reorder 支持移动）</summary>
    public int? ParentId { get; set; }

    /// <summary>排序号</summary>
    public int Sort { get; set; }

    /// <summary>是否可见</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>权限编码（按钮节点必填，两段式如 "users:edit"；需全局唯一）</summary>
    [MaxLength(100)]
    public string? PermissionCode { get; set; }

    /// <summary>绑定的后端接口（按钮节点选填，如 "UserController.Update"）</summary>
    [MaxLength(200)]
    public string? ControllerAction { get; set; }

    /// <summary>备注</summary>
    [MaxLength(200)]
    public string? Remark { get; set; }
}
