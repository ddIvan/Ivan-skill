using System;

namespace IvanProject.Models;

/// <summary>
/// 角色实体（由 ivan-db-model-gen 根据数据库表自动生成，不可手动修改）
/// 对应数据库表：Roles
/// </summary>
[Serializable()]
public partial class Roles
{
    public Roles() { }

    #region Members

    /// <summary>主键ID</summary>
    public int Id { get; set; }

    /// <summary>角色名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>角色编码</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>创建时间</summary>
    public DateTime? CreateTime { get; set; }

    #endregion
}
