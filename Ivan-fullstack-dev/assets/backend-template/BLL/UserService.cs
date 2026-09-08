using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IvanProject.Common;
using IvanProject.DAL;
using IvanProject.DTOs;
using IvanProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IvanProject.BLL;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public UserService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>登录：校验用户名密码，签发 JWT</summary>
    public async Task<LoginResult> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BusinessException("用户名或密码错误");
        }
        if (!user.IsEnabled)
        {
            throw new BusinessException("账号已被禁用");
        }

        var token = GenerateToken(user);
        return new LoginResult
        {
            Token = token,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }

    /// <summary>获取当前登录用户信息</summary>
    public async Task<LoginResult> GetProfileAsync(string userName)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName)
            ?? throw new BusinessException("用户不存在");
        return new LoginResult
        {
            Token = string.Empty,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }

    /// <summary>注册（示例）：用户名已存在则报错</summary>
    public async Task<User> RegisterAsync(string userName, string password, string? displayName)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == userName))
        {
            throw new BusinessException("用户名已存在");
        }
        var user = new User
        {
            UserName = userName,
            PasswordHash = HashPassword(password),
            DisplayName = displayName ?? userName,
            Role = "user"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role),
            new("uid", user.Id.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expireHours = int.Parse(_config["Jwt:ExpireHours"] ?? "8");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(expireHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // 密码哈希：PBKDF2（迭代 10000 次 + 随机盐）
    private static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(parts[1]));
    }
}
