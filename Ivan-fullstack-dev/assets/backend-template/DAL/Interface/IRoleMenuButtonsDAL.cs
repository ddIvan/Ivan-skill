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
/// RoleMenuButtons数据层接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IRoleMenuButtonsDAL : IBaseDAL<RoleMenuButtons>
{
    /// <summary>按角色ID查询所有按钮权限列表</summary>
    List<RoleMenuButtons> GetByRoleId(int roleId);

    /// <summary>删除指定角色+菜单的所有按钮权限</summary>
    void DeleteByRoleIdAndMenuId(int roleId, int menuId);
}
