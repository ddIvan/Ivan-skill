using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;
using IvanProject.DAL.Interface;
using IvanProject.BLL.Interface;

namespace IvanProject.BLL;

/// <summary>
/// UserRoles 业务逻辑类 — 管理用户-角色多对多关联。
/// 用户角色变更后自动清除用户权限缓存，确保下次查询从数据库重新加载。
/// </summary>
public partial class UserRolesBLL
    : BaseBLL<IUserRolesDAL, UserRoles>, IUserRolesBLL
{
    private readonly RolePermissionService? _permService;

    public UserRolesBLL(IDataSessionFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// 带权限缓存清除能力的构造函数（推荐使用）。
    /// 注入 RolePermissionService 后，SaveUserRoles 会在写入后自动清除用户级权限缓存。
    /// </summary>
    public UserRolesBLL(IDataSessionFactory factory, RolePermissionService permService) : base(factory)
    {
        _permService = permService;
    }

    /// <summary>查询用户拥有的角色列表（SQL 层 JOIN）</summary>
    public List<Roles> GetRolesByUserId(int userId)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IUserRolesDAL>().GetRolesByUserId(userId);
        }
    }

    /// <summary>查询用户拥有的角色ID列表</summary>
    public List<int> GetRoleIdsByUserId(int userId)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IUserRolesDAL>().GetRoleIdsByUserId(userId);
        }
    }

    /// <summary>保存用户角色（全量替换：DELETE + INSERT，事务保证）。保存后清除用户权限缓存。</summary>
    public void SaveUserRoles(int userId, List<int> roleIds)
    {
        using (var session = Factory.OpenSession())
        {
            session.BeginTrans();
            try
            {
                var dal = session.CreateDAL<IUserRolesDAL>();
                dal.DeleteByUserId(userId);
                foreach (var roleId in roleIds)
                {
                    dal.Insert(new UserRoles { UserId = userId, RoleId = roleId, CreateTime = DateTime.Now });
                }
                session.CommitTrans();
            }
            catch
            {
                session.RollbackTrans();
                throw;
            }
        }

        // 用户角色变更后清除用户权限缓存，确保下次查询数据一致
        _permService?.ClearUserPermissionCache(userId);
    }
}
