using Ivan.Data;
using IvanProject.BLL.Interface;
using IvanProject.Common;
using IvanProject.DAL.Interface;
using IvanProject.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IvanProject.BLL;

/// <summary>
/// 角色权限服务：管理角色与菜单/按钮权限的分配与查询。
/// 权限变更后自动清除内存缓存，下次查询时重新加载。
/// </summary>
public class RolePermissionService
{
    private readonly IDataSessionFactory _factory;
    private readonly IRolesBLL _rolesBLL;
    private readonly IMenusBLL _menusBLL;
    private readonly IMemoryCache _cache;

    public RolePermissionService(IDataSessionFactory factory, IRolesBLL rolesBLL, IMenusBLL menusBLL, IMemoryCache cache)
    {
        _factory = factory;
        _rolesBLL = rolesBLL;
        _menusBLL = menusBLL;
        _cache = cache;
    }

    /// <summary>获取角色的菜单ID列表</summary>
    public List<int> GetRoleMenuIds(int roleId)
    {
        var all = GetRoleMenus(roleId);
        return all.Select(rm => rm.MenuId).Distinct().ToList();
    }

    /// <summary>获取角色的按钮权限列表</summary>
    public List<RoleMenuButtons> GetRoleButtons(int roleId)
    {
        return GetRoleMenuButtons(roleId);
    }

    /// <summary>获取角色的按钮权限字典（menuId -> buttonKeys[]）</summary>
    public Dictionary<int, List<string>> GetRoleButtonsDict(int roleId)
    {
        var all = GetRoleMenuButtons(roleId);
        return all.GroupBy(b => b.MenuId)
                   .ToDictionary(g => g.Key, g => g.Select(b => b.ButtonKey).ToList());
    }

    /// <summary>保存角色的菜单权限（全量替换：SQL 层 DELETE + 批量 INSERT）</summary>
    public void SaveRoleMenus(int roleId, List<int> menuIds)
    {
        using (var session = _factory.OpenSession())
        {
            var dal = session.CreateDAL<IRoleMenusDAL>();
            dal.DeleteByRoleId(roleId);
            foreach (var menuId in menuIds)
            {
                dal.Insert(new RoleMenus { RoleId = roleId, MenuId = menuId });
            }
            session.Commit();
        }
        ClearPermissionCache(roleId);
    }

    /// <summary>保存角色的按钮权限（全量替换：SQL 层 DELETE + 批量 INSERT）</summary>
    public void SaveRoleButtons(int roleId, int menuId, List<string> buttonKeys)
    {
        using (var session = _factory.OpenSession())
        {
            var dal = session.CreateDAL<IRoleMenuButtonsDAL>();
            dal.DeleteByRoleIdAndMenuId(roleId, menuId);
            foreach (var key in buttonKeys)
            {
                dal.Insert(new RoleMenuButtons { RoleId = roleId, MenuId = menuId, ButtonKey = key });
            }
            session.Commit();
        }
        ClearPermissionCache(roleId);
    }

    /// <summary>检查用户是否有指定按钮权限（走精准 SQL 查询）</summary>
    public bool HasButtonPermission(string roleCode, string menuPath, string buttonKey)
    {
        var role = _rolesBLL.GetByCode(roleCode);
        if (role == null) return false;

        var menu = _menusBLL.GetByPath(menuPath);
        if (menu == null) return false;

        var all = GetRoleMenuButtons(role.Id);
        return all.Any(b => b.MenuId == menu.Id && b.ButtonKey == buttonKey);
    }

    /// <summary>获取用户有权限的菜单ID列表（走精准 SQL 查询）</summary>
    public List<int> GetUserMenuIds(string roleCode)
    {
        var role = _rolesBLL.GetByCode(roleCode);
        if (role == null) return new List<int>();
        return GetRoleMenuIds(role.Id);
    }

    private List<RoleMenus> GetRoleMenus(int roleId)
    {
        var key = $"role_menus_{roleId}";
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            using (var session = _factory.OpenSession())
            {
                var dal = session.CreateDAL<IRoleMenusDAL>();
                // 改为 DAL 精准查询：Select * from RoleMenus where RoleId = @RoleId
                // 但 GetMenuIdsByRoleId 只返回 int[]，需要完整的 RoleMenus 列表来删除。
                // 简化：直接走 DAL 的 SelectList 查询
                return dal.SelectList(
                    "SELECT * FROM RoleMenus (nolock) WHERE RoleId = @RoleId",
                    new { RoleId = roleId }).ToList();
            }
        })!;
    }

    private List<RoleMenuButtons> GetRoleMenuButtons(int roleId)
    {
        var key = $"role_buttons_{roleId}";
        return _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            using (var session = _factory.OpenSession())
            {
                var dal = session.CreateDAL<IRoleMenuButtonsDAL>();
                return dal.SelectList(
                    "SELECT * FROM RoleMenuButtons (nolock) WHERE RoleId = @RoleId",
                    new { RoleId = roleId }).ToList();
            }
        })!;
    }

    private void ClearPermissionCache(int roleId)
    {
        _cache.Remove($"role_menus_{roleId}");
        _cache.Remove($"role_buttons_{roleId}");
    }
}
