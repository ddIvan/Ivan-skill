using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;

namespace IvanProject.DAL.Interface;

/// <summary>
/// RoleMenus数据层接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IRoleMenusDAL : IBaseDAL<RoleMenus>
{
    /// <summary>按角色ID查询关联的菜单ID列表</summary>
    List<int> GetMenuIdsByRoleId(int roleId);

    /// <summary>删除指定角色的所有菜单关联</summary>
    void DeleteByRoleId(int roleId);
}
