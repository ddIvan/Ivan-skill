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
/// Users操作类
/// </summary>
public partial class UsersDAL
    : BaseDAL<Users>, IUsersDAL
{
    public UsersDAL() : base()
    {
    }

    public UsersDAL(IDbHelper dbHelper) : base(dbHelper)
    {
    }

    // 注意：Users 表不包含 Role 字段，多角色通过 UserRoles 关联表实现
    protected override string InsertSql => @"INSERT into Users (UserName, PasswordHash, DisplayName, CreateTime, IsEnabled)
                             VALUES (@UserName, @PasswordHash, @DisplayName, @CreateTime, @IsEnabled)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from Users WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE Users SET UserName=@UserName, PasswordHash=@PasswordHash, DisplayName=@DisplayName, 
                                    CreateTime=@CreateTime, IsEnabled=@IsEnabled
                                    where Id = @Id";

    protected override string SelectAllSql => @"select * from Users (nolock)";

    #region 查询

    /// <summary>
    /// 按主键查询一个对象
    /// </summary>
    public override Users Select(Users value)
    {
        string sql = "SELECT * FROM Users (nolock) WHERE Id = @Id";
        return DbHelper.Connection.Query<Users>(sql, value).FirstOrDefault();
    }

    /// <summary>按用户名精准查询（SQL 层 WHERE，禁止全表加载后内存过滤）</summary>
    public Users? GetByUserName(string userName)
    {
        string sql = "SELECT * FROM Users (nolock) WHERE UserName = @UserName";
        return DbHelper.Connection.Query<Users>(sql, new { UserName = userName }).FirstOrDefault();
    }

    /// <summary>分页搜索（SQL 层过滤 + OFFSET/FETCH 分页）</summary>
    public List<Users> Search(PageSearchModel searchModel, out int totalRecordCount)
    {
        string where = "1=1";
        object? param = null;
        var keyword = searchModel.TryGetModelValue("keyword", out var kw) ? kw : null;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where = "(UserName like '%' + @Keyword + '%' or DisplayName like '%' + @Keyword + '%')";
            param = new { Keyword = keyword };
        }

        string countSql = $"select count(1) from Users (nolock) where {where}";
        string dataSql = $"select * from Users (nolock) where {where}";

        // SelectByPage 的页码为 0-based（MsSql rownumber>page*size / MySQL limit page*size），
        // PageSearchModel.Page 为 1-based（从 1 开始），此处必须 -1，否则首页会跳过前 N 条数据
        var page = SelectByPage<Users>(dataSql, searchModel.Page - 1, searchModel.Limit, out totalRecordCount, param, "Id desc");
        return page.ToList();
    }

    #endregion
}
