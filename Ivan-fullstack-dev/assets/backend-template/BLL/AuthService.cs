using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.DTOs;
using IvanProject.Models;
using Microsoft.IdentityModel.Tokens;

namespace IvanProject.BLL;

/// <summary>
/// 用户认证服务（手动编写，注入由 ivan-db-bll-gen 生成的 IUsersBLL）
/// 
/// 前置条件：
/// 1. 数据库中已创建 Users 表
/// 2. 已通过 ivan-db-model-gen → ivan-db-dal-gen → ivan-db-bll-gen 生成 Users 的三层代码
/// 3. 生成的 BLL 接口 IUsersBLL 继承了 IBaseBLL<Users>，提供了 Create/Update/Delete 等方法
/// </summary>
public class AuthService
{
    private readonly IUsersBLL _usersBLL;
    private readonly IConfiguration _config;

    public AuthService(IUsersBLL usersBLL, IConfiguration config)
    {
        _usersBLL = usersBLL;
        _config = config;
    }

    /// <summary>登录：通过 DAL.GetByUserName 精准查询，校验用户名密码，签发 JWT</summary>
    public LoginResult Login(LoginRequest request)
    {
        var user = _usersBLL.GetByUserName(request.UserName)
            ?? throw new BusinessException("用户名或密码错误");

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new BusinessException("用户名或密码错误");
        }
        if (user.IsEnabled != true)
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

    /// <summary>获取当前登录用户信息（精准查询）</summary>
    public LoginResult GetProfile(string userName)
    {
        var user = _usersBLL.GetByUserName(userName)
            ?? throw new BusinessException("用户不存在");
        return new LoginResult
        {
            Token = string.Empty,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Role = user.Role
        };
    }

    /// <summary>注册：通过精准查询检查用户名是否已存在</summary>
    public int Register(string userName, string password, string? displayName)
    {
        if (_usersBLL.GetByUserName(userName) != null)
        {
            throw new BusinessException("用户名已存在");
        }
        var user = new Users
        {
            UserName = userName,
            PasswordHash = HashPassword(password),
            DisplayName = displayName ?? userName,
            Role = "user",
            IsEnabled = true,
            CreatedAt = DateTime.Now
        };
        // 使用 BLL 的 Create 方法（内部调用 DAL.InsertForGeneratedKey）
        var result = _usersBLL.Create(user);
        if (!result.Success)
        {
            throw new BusinessException(result.Message);
        }
        return result.Data;
    }

    private string GenerateToken(Users user)
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
