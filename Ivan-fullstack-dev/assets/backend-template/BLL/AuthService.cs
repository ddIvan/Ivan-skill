using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Ivan.Data;
using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.DAL.Interface;
using IvanProject.DTOs;
using IvanProject.Models;
using Microsoft.IdentityModel.Tokens;

namespace IvanProject.BLL;

/// <summary>
/// 用户认证服务（手动编写，注入由 ivan-db-bll-gen 生成的 IUsersBLL）
/// 
/// 前置条件：
/// 1. 数据库中已创建 Users 表 + UserRoles 关联表（支持多角色）
/// 2. 已通过 ivan-db-model-gen → ivan-db-dal-gen → ivan-db-bll-gen 生成 Users 的三层代码
/// 3. 生成的 BLL 接口 IUsersBLL 继承了 IBaseBLL<Users>，提供了 Create/Update/Delete 等方法
/// </summary>
public class AuthService
{
    private readonly IUsersBLL _usersBLL;
    private readonly IConfiguration _config;
    private readonly IDataSessionFactory _factory;

    public AuthService(IUsersBLL usersBLL, IConfiguration config, IDataSessionFactory factory)
    {
        _usersBLL = usersBLL;
        _config = config;
        _factory = factory;
    }

    /// <summary>
    /// 登录：校验用户名密码，签发含多角色的 JWT，并返回用户的菜单权限和按钮权限数据。
    /// 按钮权限以 Dictionary&lt;string, string[]&gt; 返回，key 为菜单路径（如 /users），
    /// value 为按钮 key 列表（如 ["add","edit","delete"]），供前端 userStore.hasButton() 调用。
    /// </summary>
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

        // 查询用户的所有角色编码
        var roleCodes = GetUserRoleCodes(user.Id);
        user.RoleCodes = roleCodes;

        var token = GenerateToken(user);

        // 统一权限树：汇总用户所有角色的授权节点（含目录/菜单/按钮）与按钮权限
        CollectPermissionData(user.Id, roleCodes, out var menuIds, out var buttonPermissions);

        return new LoginResult
        {
            Token = token,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Roles = roleCodes,
            MenuIds = menuIds,
            ButtonPermissions = buttonPermissions
        };
    }

    /// <summary>获取当前登录用户信息（包含多角色与权限汇总，前端刷新页面恢复登录态依赖）</summary>
    public LoginResult GetProfile(string userName)
    {
        var user = _usersBLL.GetByUserName(userName)
            ?? throw new BusinessException("用户不存在");

        var roleCodes = GetUserRoleCodes(user.Id);

        // 与登录一致的权限汇总：授权节点全集 + 按钮权限映射
        CollectPermissionData(user.Id, roleCodes, out var menuIds, out var buttonPermissions);

        return new LoginResult
        {
            Token = string.Empty,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Roles = roleCodes,
            MenuIds = menuIds,
            ButtonPermissions = buttonPermissions
        };
    }

    /// <summary>
    /// 统一权限树：汇总用户所有角色的授权节点（含目录/菜单/按钮）与按钮权限映射。
    /// 登录与 profile 共用；admin 角色取全量（含未授权节点）。
    /// </summary>
    private void CollectPermissionData(int userId, List<string> roleCodes, out List<int> menuIds, out Dictionary<string, List<string>> buttonPermissions)
    {
        menuIds = new List<int>();
        buttonPermissions = new Dictionary<string, List<string>>();

        using (var session = _factory.OpenSession())
        {
            var conn = session.CreateDAL<DAL.Interface.IRoleMenusDAL>().DbHelper.Connection;

            // 1. 授权节点全集（多角色合并）：目录/菜单/按钮节点 ID（供前端角色权限树回显）
            menuIds = conn.Query<int>(
                @"SELECT DISTINCT rm.MenuId FROM RoleMenus rm (nolock)
                  JOIN UserRoles ur (nolock) ON ur.RoleId = rm.RoleId
                  WHERE ur.UserId = @UserId", new { UserId = userId }).ToList();

            // 2. 按钮权限：按钮节点（MenuType=3）PermissionCode 的 action 段 → 归属其父菜单 Path
            //    如 users:edit → 父菜单 /users → buttonPermissions["/users"] += ["edit"]
            //    （前端 v-perm 按钮显隐使用；view 段由页面权限兜底补充）
            //    关键：按钮节点 m3 必须自身被角色勾选（rm3 JOIN RoleMenus）。
            //    若只按父菜单勾选推导按钮，会导致角色仅勾选菜单时其下所有按钮都被判为有权限，
            //    前端 v-perm 显隐失效（按钮仍显示），与后端 [RequirePerm] 校验结果不一致。
            var btnRows = conn.Query<(string Path, string Code)>(
                @"SELECT DISTINCT m2.Path, m3.PermissionCode
                  FROM UserRoles ur (nolock)
                  JOIN RoleMenus rm (nolock) ON rm.RoleId = ur.RoleId
                  JOIN Menus m2 (nolock) ON m2.Id = rm.MenuId AND m2.MenuType = 2
                  JOIN Menus m3 (nolock) ON m3.ParentId = m2.Id AND m3.MenuType = 3 AND m3.IsEnabled = 1
                  JOIN RoleMenus rm3 (nolock) ON rm3.RoleId = rm.RoleId AND rm3.MenuId = m3.Id
                  WHERE ur.UserId = @UserId", new { UserId = userId }).ToList();

            foreach (var row in btnRows)
            {
                if (string.IsNullOrEmpty(row.Path) || string.IsNullOrEmpty(row.Code)) continue;
                var action = row.Code.Contains(':') ? row.Code.Substring(row.Code.IndexOf(':') + 1) : row.Code;
                if (!buttonPermissions.TryGetValue(row.Path, out var keys))
                    buttonPermissions[row.Path] = keys = new List<string>();
                if (!keys.Contains(action)) keys.Add(action);
            }
        }

        // 3. admin 角色拥有全部菜单与按钮权限（含未授权节点）
        if (roleCodes.Contains("admin"))
        {
            using (var session = _factory.OpenSession())
            {
                var menusDAL = session.CreateDAL<DAL.Interface.IMenusDAL>();
                var allMenus = menusDAL.SelectAll();
                menuIds = allMenus.Select(m => m.Id).ToList();

                foreach (var menu in allMenus.Where(m => m.MenuType == 2))
                {
                    if (string.IsNullOrEmpty(menu.Path)) continue;
                    var keys = new List<string>();
                    foreach (var btn in allMenus.Where(b => b.ParentId == menu.Id && b.MenuType == 3))
                    {
                        var code = btn.PermissionCode ?? "";
                        var action = code.Contains(':') ? code.Substring(code.IndexOf(':') + 1) : code;
                        if (!string.IsNullOrEmpty(action) && !keys.Contains(action)) keys.Add(action);
                    }
                    // view 兜底：页面级 [RequirePerm("{path}:view")] 与前端按钮显隐都依赖它
                    if (!keys.Contains("view")) keys.Insert(0, "view");
                    buttonPermissions[menu.Path] = keys;
                }
            }
        }
    }

    /// <summary>注册：创建用户并赋予默认角色（"user"）</summary>
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
            IsEnabled = true,
            CreateTime = DateTime.Now
        };
        var result = _usersBLL.Create(user);
        if (!result.Success)
        {
            throw new BusinessException(result.Message);
        }

        var userId = result.Data;

        // 注册后自动赋予 "user" 默认角色
        using (var session = _factory.OpenSession())
        {
            var rolesDAL = session.CreateDAL<IRolesDAL>();
            var defaultRole = rolesDAL.GetByCode("user");
            if (defaultRole != null)
            {
                session.BeginTrans();
                try
                {
                    var userRolesDAL = session.CreateDAL<IUserRolesDAL>();
                    userRolesDAL.Insert(new UserRoles { UserId = userId, RoleId = defaultRole.Id, CreateTime = DateTime.Now });
                    session.CommitTrans();
                }
                catch
                {
                    session.RollbackTrans();
                    throw;
                }
            }
        }

        return userId;
    }

    /// <summary>查询用户的所有角色编码（SQL 层 JOIN）</summary>
    private List<string> GetUserRoleCodes(int userId)
    {
        using (var session = _factory.OpenSession())
        {
            return session.CreateDAL<IUserRolesDAL>().GetRolesByUserId(userId)
                .Select(r => r.Code)
                .ToList();
        }
    }

    private string GenerateToken(Users user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new("uid", user.Id.ToString())
        };

        // 多角色：每个角色一个 ClaimTypes.Role Claim（ASP.NET Core Authorize 原生支持多 Claim）
        foreach (var roleCode in user.RoleCodes)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleCode));
        }

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
    // 注意：存储格式 base64(salt).base64(hash)，绝不能存 BCrypt（$2a$ 开头）格式，否则登录必失败
    public static string HashPassword(string password)
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
