namespace IvanProject.Models;

/// <summary>
/// UserRoles 的 partial 扩展类（手动扩展字段/方法）
/// </summary>
public partial class UserRoles
{
    /// <summary>关联的角色对象（非数据库字段，查询时手动填充）</summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Roles? Role { get; set; }
}
