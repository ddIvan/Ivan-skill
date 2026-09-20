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
/// Users操作接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IUsersBLL : IBaseBLL<Users>
{
    PageResultModel<List<Users>> List(PageSearchModel searchModel);
    List<Users> GetALL();
    ResultModel<int> Create(Users model);
    Users? GetById(int id);
    Users? GetByUserName(string userName);
    ResultModel Modify(Users model);
    ResultModel Remove(int id);

    /// <summary>获取用户的角色ID列表（多角色支持）</summary>
    List<int> GetUserRoleIds(int userId);

    /// <summary>获取用户的完整角色列表（多角色支持）</summary>
    List<Roles> GetUserRoles(int userId);

    /// <summary>保存用户-角色关联（全量替换：DELETE + INSERT，事务保证）</summary>
    void SaveUserRoles(int userId, List<int> roleIds);
}
