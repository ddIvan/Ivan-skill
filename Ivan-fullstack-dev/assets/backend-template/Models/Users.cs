namespace IvanProject.Models;

/// <summary>
/// Users 的 partial 扩展类（手动扩展字段/方法）
/// 支持多角色：不需要在 Users 表中存储 Role 字段，通过 UserRoles 关联表实现多对多
/// </summary>
public partial class Users
{
    /// <summary>用户拥有的角色列表（非数据库字段，查询时手动填充）</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<Roles> Roles { get; set; } = new();

    /// <summary>用户拥有的角色编码列表（非数据库字段，查询时手动填充）</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<string> RoleCodes { get; set; } = new();
}
