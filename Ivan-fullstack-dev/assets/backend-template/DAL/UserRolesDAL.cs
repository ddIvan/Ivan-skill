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
/// UserRoles 数据访问层 — 管理用户-角色多对多关联
/// 参考 RoleMenusDAL 的模式：SQL 层精准查询，禁止全表加载后内存过滤
/// </summary>
public partial class UserRolesDAL
    : BaseDAL<UserRoles>, IUserRolesDAL
{
    public UserRolesDAL() : base()
    {
    }

    public UserRolesDAL(IDbHelper dbHelper) : base(dbHelper)
    {
    }

    protected override string InsertSql => @"INSERT into UserRoles (UserId, RoleId, CreateTime)
                             VALUES (@UserId, @RoleId, @CreateTime)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from UserRoles WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE UserRoles SET UserId=@UserId, RoleId=@RoleId where Id = @Id";

    protected override string SelectAllSql => @"select * from UserRoles (nolock)";

    #region 查询

    public override UserRoles Select(UserRoles value)
    {
        string sql = "SELECT * FROM UserRoles (nolock) WHERE Id = @Id";
        return DbHelper.Connection.Query<UserRoles>(sql, value).FirstOrDefault();
    }

    /// <summary>查询用户拥有的角色ID列表（SQL 层过滤）</summary>
    public List<int> GetRoleIdsByUserId(int userId)
    {
        string sql = "SELECT RoleId FROM UserRoles (nolock) WHERE UserId = @UserId";
        return DbHelper.Connection.Query<UserRoles>(sql, new { UserId = userId }).Select(ur => ur.RoleId).ToList();
    }

    /// <summary>查询用户拥有的完整角色列表（JOIN Roles 表）</summary>
    public List<Roles> GetRolesByUserId(int userId)
    {
        string sql = @"SELECT r.* FROM Roles r (nolock)
                       INNER JOIN UserRoles ur (nolock) ON r.Id = ur.RoleId
                       WHERE ur.UserId = @UserId";
        return DbHelper.Connection.Query<Roles>(sql, new { UserId = userId }).ToList();
    }

    /// <summary>删除用户的所有角色关联（SQL 层 DELETE）</summary>
    public void DeleteByUserId(int userId)
    {
        string sql = "DELETE FROM UserRoles WHERE UserId = @UserId";
        ExecuteSql(sql, new { UserId = userId });
    }

    #endregion
}
