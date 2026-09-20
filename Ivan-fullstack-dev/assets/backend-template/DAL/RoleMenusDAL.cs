using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Common;
using Dapper;
using IvanProject.Models;
using IvanProject.DAL.Interface;

namespace IvanProject.DAL;

/// <summary>
/// RoleMenus操作类
/// </summary>
public partial class RoleMenusDAL
    : BaseDAL<RoleMenus>, IRoleMenusDAL
{
    public RoleMenusDAL() : base()
    {
    }

    public RoleMenusDAL(IDbHelper dbHelper) : base(dbHelper)
    {
    }

    protected override string InsertSql => @"INSERT into RoleMenus (RoleId, MenuId)
                             VALUES (@RoleId, @MenuId)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from RoleMenus WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE RoleMenus SET RoleId=@RoleId, MenuId=@MenuId where Id = @Id";

    protected override string SelectAllSql => @"select * from RoleMenus (nolock)";

    #region 查询

    public override RoleMenus Select(RoleMenus value)
    {
        string sql = "SELECT * FROM RoleMenus (nolock) WHERE Id = @Id";
        return DbHelper.Connection.Query<RoleMenus>(sql, value).FirstOrDefault();
    }

    /// <summary>按角色ID查询关联的菜单ID列表（SQL 层过滤）</summary>
    public List<int> GetMenuIdsByRoleId(int roleId)
    {
        string sql = "SELECT MenuId FROM RoleMenus (nolock) WHERE RoleId = @RoleId";
        return DbHelper.Connection.Query<RoleMenus>(sql, new { RoleId = roleId }).Select(r => r.MenuId).ToList();
    }

    /// <summary>删除指定角色的所有菜单关联（SQL 层 DELETE）</summary>
    public void DeleteByRoleId(int roleId)
    {
        string sql = "DELETE FROM RoleMenus WHERE RoleId = @RoleId";
        ExecuteSql(sql, new { RoleId = roleId });
    }

    #endregion
}
