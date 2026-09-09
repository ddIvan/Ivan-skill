namespace IvanProject.Models;

/// <summary>
/// Menus 的 partial 扩展类（手动扩展字段/方法）
/// </summary>
public partial class Menus
{
    /// <summary>子菜单列表（树形结构用，非数据库字段）</summary>
    public List<Menus> Children { get; set; } = new();
}
