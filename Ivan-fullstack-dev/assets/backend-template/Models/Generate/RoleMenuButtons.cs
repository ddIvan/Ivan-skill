using System;

namespace IvanProject.Models;

/// <summary>
/// 角色菜单按钮权限实体（由 ivan-db-model-gen 根据数据库表自动生成，不可手动修改）
/// 对应数据库表：RoleMenuButtons
/// </summary>
[Serializable()]
public partial class RoleMenuButtons
{
    public RoleMenuButtons() { }

    #region Members

    /// <summary>主键ID</summary>
    public int Id { get; set; }

    /// <summary>角色ID</summary>
    public int RoleId { get; set; }

    /// <summary>菜单ID</summary>
    public int MenuId { get; set; }

    /// <summary>按钮权限标识（如 add/edit/delete/export）</summary>
    public string ButtonKey { get; set; } = string.Empty;

    #endregion
}
