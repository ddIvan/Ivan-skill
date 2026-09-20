using System.ComponentModel.DataAnnotations;

namespace IvanProject.DTOs;

/// <summary>管理员更新用户信息请求（支持多角色）</summary>
public class UserUpdateRequest
{
    [Required]
    public int Id { get; set; }

    [MaxLength(50)]
    public string? DisplayName { get; set; }

    /// <summary>角色ID列表（多角色支持，全量替换；null 表示不修改角色）</summary>
    public List<int>? RoleIds { get; set; }

    public bool? IsEnabled { get; set; }

    /// <summary>填写则重置密码（不少于 6 位）</summary>
    [MinLength(6)]
    public string? NewPassword { get; set; }
}
