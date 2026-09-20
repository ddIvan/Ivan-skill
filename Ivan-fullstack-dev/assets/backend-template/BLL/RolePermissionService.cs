using Dapper;
using Ivan.Data;
using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.DAL.Interface;
using IvanProject.Models;

namespace IvanProject.BLL;

/// <summary>
/// 角色权限服务（统一权限树模型）。
/// 按钮权限 = 菜单树节点（Menus.MenuType = 3），与目录/菜单在同一棵树中管理；
/// 授权统一存于 RoleMenus（勾选树节点即授权），无独立的 MenuActions / RoleMenuButtons 表。
///
/// 权限编码：按钮节点的 Menus.PermissionCode（两段式 {resource}:{action}，如 "users:edit"），
/// 后端 [RequirePerm] 校验与前端 v-perm 指令显隐共用同一编码。
///
/// 缓存策略：
/// - 角色级权限缓存（role_menus_{roleId} / role_perm_codes_{roleId}）——角色权限变更时清除；
/// - 用户级权限缓存（user_perms_{userId}）——用户角色关系变更时清除，避免每次请求都查库。
/// </summary>
public class RolePermissionService
{
    private readonly IDataSessionFactory _factory;
    private readonly IRolesBLL _rolesBLL;
    private readonly IMenusBLL _menusBLL;
    private readonly ICacheService _cache;

    private static readonly TimeSpan RolePermissionExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan UserPermissionExpiration = TimeSpan.FromMinutes(10);

    public RolePermissionService(IDataSessionFactory factory, IRolesBLL rolesBLL, IMenusBLL menusBLL, ICacheService cache)
    {
        _factory = factory;
        _rolesBLL = rolesBLL;
        _menusBLL = menusBLL;
        _cache = cache;
    }

    // ==================== 角色权限查询 ====================

    /// <summary>获取角色的菜单/按钮节点ID列表（统一树：含 MenuType=3 按钮节点）</summary>
    public List<int> GetRoleMenuIds(int roleId)
    {
        return GetRoleMenus(roleId).Select(rm => rm.MenuId).Distinct().ToList();
    }

    /// <summary>获取角色的按钮权限编码列表（角色勾选的所有按钮节点的 PermissionCode）</summary>
    public List<string> GetRolePermissionCodeList(int roleId)
    {
        return GetRolePermissionCodes(roleId).ToList();
    }

    // ==================== 角色权限保存 ====================

    /// <summary>保存角色的菜单权限（全量替换：DELETE + INSERT，事务保证）。
    /// 统一树模型下按钮节点与菜单节点同存于 menuIds，一次保存即可。</summary>
    public void SaveRoleMenus(int roleId, List<int> menuIds)
    {
        using (var session = _factory.OpenSession())
        {
            session.BeginTrans();
            try
            {
                var dal = session.CreateDAL<IRoleMenusDAL>();
                dal.DeleteByRoleId(roleId);
                foreach (var menuId in menuIds)
                {
                    dal.Insert(new RoleMenus { RoleId = roleId, MenuId = menuId });
                }
                session.CommitTrans();
            }
            catch
            {
                session.RollbackTrans();
                throw;
            }
        }
        ClearRolePermissionCache(roleId);
    }

    // ==================== 权限校验 ====================

    /// <summary>
    /// 检查用户（通过角色编码）是否拥有声明权限集合中的任一权限。
    /// 用于配合 <see cref="PermAuthorizationFilter"/> 进行 [RequirePerm] 注解校验。
    /// </summary>
    /// <param name="roleCode">角色编码</param>
    /// <param name="requiredPermissions">声明的权限编码数组，例如 ["users:edit", "users:delete"]</param>
    /// <returns>是否拥有任一所需权限</returns>
    public bool HasAnyPermission(string roleCode, string[] requiredPermissions)
    {
        if (requiredPermissions == null || requiredPermissions.Length == 0) return true;

        var role = _rolesBLL.GetByCode(roleCode);
        if (role == null) return false;

        // 获取该角色拥有的所有权限编码（缓存）
        var ownedPerms = GetRolePermissionCodes(role.Id);
        return requiredPermissions.Any(p => ownedPerms.Contains(p));
    }

    /// <summary>
    /// 异步版本——检查用户（通过角色编码集合）是否拥有声明权限集合中的任一权限。
    /// PermAuthorizationFilter 中使用。
    /// </summary>
    public Task<bool> HasAnyPermissionAsync(string roleCode, string[] requiredPermissions)
    {
        return Task.FromResult(HasAnyPermission(roleCode, requiredPermissions));
    }

    /// <summary>获取用户有权限的菜单ID列表（走精准 SQL 查询）</summary>
    public List<int> GetUserMenuIds(string roleCode)
    {
        var role = _rolesBLL.GetByCode(roleCode);
        if (role == null) return new List<int>();
        return GetRoleMenuIds(role.Id);
    }

    // ==================== 用户级缓存 ====================

    /// <summary>
    /// 获取用户所有角色的权限编码合集（缓存）。
    /// 缓存键：user_perms_{userId}
    /// 当用户角色关系变更时需清除此缓存（参见 <see cref="ClearUserPermissionCache"/>）。
    /// </summary>
    public HashSet<string> GetUserAllPermissionCodes(int userId)
    {
        var key = $"user_perms_{userId}";
        return _cache.GetOrCreate(key, () =>
        {
            using (var session = _factory.OpenSession())
            {
                // 查用户的所有角色编码
                var userRolesDAL = session.CreateDAL<IUserRolesDAL>();
                var roles = userRolesDAL.GetRolesByUserId(userId);
                if (roles == null || roles.Count == 0) return new HashSet<string>();

                // 汇总所有角色的权限编码
                var result = new HashSet<string>();
                foreach (var role in roles)
                {
                    result.UnionWith(GetRolePermissionCodes(role.Id));
                }
                return result;
            }
        }, UserPermissionExpiration);
    }

    /// <summary>清除用户权限缓存（用户角色变更时调用）</summary>
    public void ClearUserPermissionCache(int userId)
    {
        _cache.Remove($"user_perms_{userId}");
    }

    /// <summary>
    /// 清除角色关联的所有缓存（角色权限变更时调用）：
    /// 1. 角色自身权限缓存
    /// 2. 拥有该角色的所有用户的权限缓存（级联失效）
    /// </summary>
    public void ClearRolePermissionCache(int roleId)
    {
        // 清除角色自身权限缓存
        _cache.Remove($"role_menus_{roleId}");
        _cache.Remove($"role_perm_codes_{roleId}");

        // 级联清除拥有该角色的所有用户权限缓存
        using (var session = _factory.OpenSession())
        {
            var userRolesDAL = session.CreateDAL<IUserRolesDAL>();
            // 通过 DbHelper.Query 查询该角色的所有用户ID
            var userRoles = userRolesDAL.DbHelper.Connection.Query<UserRoles>(
                "SELECT DISTINCT UserId FROM UserRoles (nolock) WHERE RoleId = @RoleId",
                new { RoleId = roleId }).ToList();
            foreach (var ur in userRoles)
            {
                _cache.Remove($"user_perms_{ur.UserId}");
            }
        }
    }

    // ==================== 内部缓存查询 ====================

    /// <summary>
    /// 清除全部角色的权限缓存（含级联用户缓存）。
    /// 菜单/按钮节点（PermissionCode、ControllerAction、Path）等影响权限编码的配置变更时调用。
    /// </summary>
    public void ClearAllRolePermissionCaches()
    {
        foreach (var role in _rolesBLL.GetALL())
        {
            ClearRolePermissionCache(role.Id);
        }
    }

    private List<RoleMenus> GetRoleMenus(int roleId)
    {
        var key = $"role_menus_{roleId}";
        return _cache.GetOrCreate(key, () =>
        {
            using (var session = _factory.OpenSession())
            {
                var dal = session.CreateDAL<IRoleMenusDAL>();
                return dal.DbHelper.Connection.Query<RoleMenus>(
                    "SELECT * FROM RoleMenus (nolock) WHERE RoleId = @RoleId",
                    new { RoleId = roleId }).ToList();
            }
        }, RolePermissionExpiration);
    }

    /// <summary>
    /// 获取角色拥有的所有权限编码（统一树模型）。
    /// 直接取角色勾选的按钮节点（MenuType=3）的 PermissionCode；
    /// 若按钮节点未配置 PermissionCode，则回退为「父级菜单路径派生 resource + ":" + 节点名拼音不可用」——
    /// 因此约定按钮节点必须配置 PermissionCode（种子数据已保证）。
    /// </summary>
    private HashSet<string> GetRolePermissionCodes(int roleId)
    {
        var key = $"role_perm_codes_{roleId}";
        return _cache.GetOrCreate(key, () =>
        {
            var result = new HashSet<string>();
            using (var session = _factory.OpenSession())
            {
                var conn = session.CreateDAL<IRoleMenusDAL>().DbHelper.Connection;
                var buttons = conn.Query<Menus>(
                    @"SELECT m.* FROM RoleMenus rm (nolock)
                      JOIN Menus m (nolock) ON m.Id = rm.MenuId
                      WHERE rm.RoleId = @RoleId AND m.MenuType = 3 AND m.IsEnabled = 1",
                    new { RoleId = roleId }).ToList();

                foreach (var btn in buttons)
                {
                    if (!string.IsNullOrWhiteSpace(btn.PermissionCode))
                        result.Add(btn.PermissionCode);
                }
            }
            return result;
        }, RolePermissionExpiration);
    }
}
