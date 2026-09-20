using System;

namespace IvanProject.Models;

/// <summary>
/// 菜单实体（统一权限树模型，由 ivan-db-model-gen 根据数据库表自动生成）
/// 对应数据库表：Menus
/// 
/// 存储方案：邻接表 (ParentId) + 路径枚举 (FullPath)
///   - ParentId 保留用于快速插入/移动单节点
///   - FullPath 如 /1/3/7，用于子树查询 LIKE '/1/3/%'
///   - Level 冗余层级，根节点=0
/// 
/// MenuType：1=目录 2=菜单(页面) 3=按钮
/// 按钮节点携带 PermissionCode（权限编码）与 ControllerAction（绑定的后端接口）
/// </summary>
[Serializable()]
public partial class Menus
{
    public Menus() { }

    #region Members

    /// <summary>主键ID</summary>
    public int Id { get; set; }

    /// <summary>菜单名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>路由路径（叶子节点必填，目录节点可为空）</summary>
    public string? Path { get; set; }

    /// <summary>菜单图标</summary>
    public string? Icon { get; set; }

    /// <summary>父级菜单ID（null 为一级菜单/根节点）</summary>
    public int? ParentId { get; set; }

    /// <summary>祖先路径（如 /1/3/7），子树查询加速</summary>
    public string? FullPath { get; set; }

    /// <summary>层级深度（根节点=0，一级子节点=1，以此类推）</summary>
    public int Level { get; set; }

    /// <summary>排序号（同级节点内排序）</summary>
    public int Sort { get; set; }

    /// <summary>是否可见（隐藏菜单仍可访问）</summary>
    public bool? IsVisible { get; set; }

    /// <summary>是否启用（禁用后递归禁用子孙在菜单树的显示）</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>节点类型：1=目录 2=菜单(页面) 3=按钮（统一权限树模型）</summary>
    public int MenuType { get; set; }

    /// <summary>权限编码（按钮节点必填，两段式如 "users:edit"；后端 RequirePerm 与前端 v-perm 共用）</summary>
    public string? PermissionCode { get; set; }

    /// <summary>绑定的后端接口（按钮节点动态维护，如 "UserController.Update"）</summary>
    public string? ControllerAction { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    #endregion
}
