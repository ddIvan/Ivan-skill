namespace IvanProject.DTOs;

public class LoginResult
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = string.Empty;
    public List<int> MenuIds { get; set; } = new();
    public Dictionary<string, List<string>> ButtonPermissions { get; set; } = new();
}
