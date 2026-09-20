using System;

namespace IvanProject.Models;

/// <summary>
/// 用户-角色关联实体（多对多，对应数据库表：UserRoles）
/// 由 ivan-db-model-gen 根据数据库表自动生成，不可手动修改
/// </summary>
[Serializable()]
public partial class UserRoles
{
    public UserRoles() { }

    #region Members

    /// <summary>主键ID</summary>
    public int Id { get; set; }

    /// <summary>用户ID（关联 Users.Id）</summary>
    public int UserId { get; set; }

    /// <summary>角色ID（关联 Roles.Id）</summary>
    public int RoleId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    #endregion
}
