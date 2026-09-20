using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;

namespace IvanProject.DAL.Interface;

/// <summary>
/// UserRoles 数据层接口 — 管理用户-角色多对多关联
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IUserRolesDAL : IBaseDAL<UserRoles>
{
    /// <summary>查询用户拥有的角色ID列表（SQL 层 WHERE，禁止全表加载）</summary>
    List<int> GetRoleIdsByUserId(int userId);

    /// <summary>查询用户拥有的完整角色列表（JOIN Roles 表）</summary>
    List<Roles> GetRolesByUserId(int userId);

    /// <summary>删除用户的所有角色关联</summary>
    void DeleteByUserId(int userId);
}
