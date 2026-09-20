namespace IvanProject.DTOs;

/// <summary>
/// 保存角色菜单权限请求（统一权限树：MenuIds 含目录/菜单/按钮节点）
/// </summary>
public class RoleMenuPermissionRequest
{
    public int RoleId { get; set; }
    public List<int> MenuIds { get; set; } = new();
}
