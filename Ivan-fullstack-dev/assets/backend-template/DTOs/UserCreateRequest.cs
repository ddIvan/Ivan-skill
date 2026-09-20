using System.ComponentModel.DataAnnotations;

namespace IvanProject.DTOs;

/// <summary>管理员新增用户请求（支持多角色）</summary>
public class UserCreateRequest
{
    [Required, MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>初始密码（不少于 6 位）</summary>
    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? DisplayName { get; set; }

    /// <summary>角色ID列表（多角色支持；null/空则赋予默认角色 user）</summary>
    public List<int>? RoleIds { get; set; }
}
