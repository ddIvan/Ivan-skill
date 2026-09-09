using System;

namespace IvanProject.Models;

/// <summary>
/// 角色菜单关联实体（由 ivan-db-model-gen 根据数据库表自动生成，不可手动修改）
/// 对应数据库表：RoleMenus
/// </summary>
[Serializable()]
public partial class RoleMenus
{
    public RoleMenus() { }

    #region Members

    /// <summary>主键ID</summary>
    public int Id { get; set; }

    /// <summary>角色ID</summary>
    public int RoleId { get; set; }

    /// <summary>菜单ID</summary>
    public int MenuId { get; set; }

    #endregion
}
