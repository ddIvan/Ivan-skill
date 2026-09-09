using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Common;
using Dapper;
using Ivan.Data.SQLBuilder;
using IvanProject.Models;
using IvanProject.DAL.Interface;

namespace IvanProject.DAL;

/// <summary>
/// RoleMenuButtons操作类
/// </summary>
public partial class RoleMenuButtonsDAL
    : BaseDAL<RoleMenuButtons>, IRoleMenuButtonsDAL
{
    public RoleMenuButtonsDAL() : base()
    {
    }

    public RoleMenuButtonsDAL(IDbHelper dbHelper) : base(dbHelper)
    {
    }

    protected override string InsertSql => @"INSERT into RoleMenuButtons (RoleId, MenuId, ButtonKey)
                             VALUES (@RoleId, @MenuId, @ButtonKey)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from RoleMenuButtons WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE RoleMenuButtons SET RoleId=@RoleId, MenuId=@MenuId, ButtonKey=@ButtonKey 
                                    where Id = @Id";

    protected override string SelectAllSql => @"select * from RoleMenuButtons (nolock)";

    #region 查询

    public override RoleMenuButtons Select(RoleMenuButtons value)
    {
        string sql = "SELECT * FROM RoleMenuButtons (nolock) WHERE Id = @Id";
        return SelectFirst(sql, value);
    }

    /// <summary>按角色ID查询按钮权限列表（SQL 层过滤）</summary>
    public List<RoleMenuButtons> GetByRoleId(int roleId)
    {
        string sql = "SELECT * FROM RoleMenuButtons (nolock) WHERE RoleId = @RoleId";
        return SelectList(sql, new { RoleId = roleId }).ToList();
    }

    /// <summary>删除指定角色+菜单的所有按钮权限（SQL 层 DELETE）</summary>
    public void DeleteByRoleIdAndMenuId(int roleId, int menuId)
    {
        string sql = "DELETE FROM RoleMenuButtons WHERE RoleId = @RoleId AND MenuId = @MenuId";
        ExecuteSql(sql, new { RoleId = roleId, MenuId = menuId });
    }

    #endregion
}
