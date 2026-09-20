using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;

namespace IvanProject.BLL.Interface;

/// <summary>
/// UserRoles 业务逻辑接口 — 管理用户-角色多对多关联
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IUserRolesBLL : IBaseBLL<UserRoles>
{
    /// <summary>查询用户拥有的角色列表</summary>
    List<Roles> GetRolesByUserId(int userId);

    /// <summary>查询用户拥有的角色ID列表</summary>
    List<int> GetRoleIdsByUserId(int userId);

    /// <summary>保存用户角色（全量替换：DELETE + INSERT，事务保证）</summary>
    void SaveUserRoles(int userId, List<int> roleIds);
}
