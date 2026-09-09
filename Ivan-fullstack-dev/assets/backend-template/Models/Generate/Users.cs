using System;

namespace IvanProject.Models;

/// <summary>
/// 用户实体（由 ivan-db-model-gen 根据数据库表自动生成，不可手动修改）
/// 对应数据库表：Users
/// </summary>
[Serializable()]
public partial class Users
{
    public Users() { }

    #region Members

    /// <summary>主键ID</summary>
    public int Id { get; set; }

    /// <summary>用户名</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>密码哈希</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>显示名称</summary>
    public string? DisplayName { get; set; }

    /// <summary>角色</summary>
    public string Role { get; set; } = "user";

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    /// <summary>是否启用</summary>
    public bool? IsEnabled { get; set; }

    #endregion
}
