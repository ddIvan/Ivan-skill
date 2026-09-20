namespace IvanProject.DTOs;

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    /// <summary>用户拥有的角色编码列表（支持多角色）</summary>
    public List<string> Roles { get; set; } = new();

    public List<int> MenuIds { get; set; } = new();
    public Dictionary<string, List<string>> ButtonPermissions { get; set; } = new();
}
