using System;

namespace IvanProject.Models;

/// <summary>
/// 菜单实体（由 ivan-db-model-gen 根据数据库表自动生成，不可手动修改）
/// 对应数据库表：Menus
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

    /// <summary>路由路径</summary>
    public string? Path { get; set; }

    /// <summary>菜单图标</summary>
    public string? Icon { get; set; }

    /// <summary>父级菜单ID（null 为一级菜单）</summary>
    public int? ParentId { get; set; }

    /// <summary>排序号</summary>
    public int Sort { get; set; }

    /// <summary>是否可见（隐藏菜单仍可访问）</summary>
    public bool? IsVisible { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    #endregion
}
