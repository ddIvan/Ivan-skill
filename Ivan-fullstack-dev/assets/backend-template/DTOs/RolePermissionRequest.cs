namespace IvanProject.DTOs;

/// <summary>
/// 保存角色菜单权限请求
/// </summary>
public class RoleMenuPermissionRequest
{
    public int RoleId { get; set; }
    public List<int> MenuIds { get; set; } = new();
}

/// <summary>
/// 保存角色按钮权限请求
/// </summary>
public class RoleButtonPermissionRequest
{
    public int RoleId { get; set; }
    public int MenuId { get; set; }
    public List<string> ButtonKeys { get; set; } = new();
}
