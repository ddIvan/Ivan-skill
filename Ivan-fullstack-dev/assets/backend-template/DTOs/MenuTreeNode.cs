namespace IvanProject.DTOs;

/// <summary>菜单树节点（前端渲染用，统一权限树：目录/菜单/按钮）</summary>
public class MenuTreeNode
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>节点类型：1=目录 2=菜单(页面) 3=按钮</summary>
    public int MenuType { get; set; }

    public string? Path { get; set; }
    public string? Icon { get; set; }
    public int? ParentId { get; set; }
    public string? FullPath { get; set; }
    public int Level { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>权限编码（按钮节点，两段式如 "users:edit"；前端 v-perm 与后端 RequirePerm 共用）</summary>
    public string? PermissionCode { get; set; }

    /// <summary>绑定的后端接口（按钮节点，如 "UserController.Update"）</summary>
    public string? ControllerAction { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    public bool HasChildren { get; set; }
    public List<MenuTreeNode> Children { get; set; } = new();
}
