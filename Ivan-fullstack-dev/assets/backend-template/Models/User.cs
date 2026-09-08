using System.ComponentModel.DataAnnotations;

namespace IvanProject.Models;

/// <summary>
/// 用户实体（示例模型，可按业务扩展）
/// </summary>
public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>密码哈希（禁止存明文）</summary>
    [Required, MaxLength(200)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? DisplayName { get; set; }

    [MaxLength(20)]
    public string Role { get; set; } = "user";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsEnabled { get; set; } = true;
}
