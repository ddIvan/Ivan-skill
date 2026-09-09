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

    protected override string InsertSql => @"INSERT into Users (UserName, PasswordHash, DisplayName, Role, CreatedAt, IsEnabled)
                             VALUES (@UserName, @PasswordHash, @DisplayName, @Role, @CreatedAt, @IsEnabled)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from Users WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE Users SET UserName=@UserName, PasswordHash=@PasswordHash, DisplayName=@DisplayName, 
                                    Role=@Role, CreatedAt=@CreatedAt, IsEnabled=@IsEnabled
                                    where Id = @Id";

    protected override string SelectAllSql => @"select * from Users (nolock)";

    #region 查询

    /// <summary>
    /// 按主键查询一个对象
    /// </summary>
    public override Users Select(Users value)
    {
        string sql = "SELECT * FROM Users (nolock) WHERE Id = @Id";
        return SelectFirst(sql, value);
    }

    /// <summary>按用户名精准查询（SQL 层 WHERE，禁止全表加载后内存过滤）</summary>
    public Users? GetByUserName(string userName)
    {
        string sql = "SELECT * FROM Users (nolock) WHERE UserName = @UserName";
        return SelectFirst(sql, new { UserName = userName });
    }

    /// <summary>分页搜索（SQL 层分页，OFFSET/FETCH）</summary>
    public List<Users> Search(PageSearchModel searchModel, out int totalRecordCount)
    {
        var builder = new SQLBuilder("select count(1) from Users (nolock)", SqlType);
        if (!string.IsNullOrWhiteSpace(searchModel.Keyword))
        {
            builder.AddWhere("(UserName like '%' + @Keyword + '%' or DisplayName like '%' + @Keyword + '%')");
        }
        totalRecordCount = CountBySql(builder.SQL, builder.GetDynamicParameters(searchModel));

        builder = new SQLBuilder("select * from Users (nolock)", SqlType);
        if (!string.IsNullOrWhiteSpace(searchModel.Keyword))
        {
            builder.AddWhere("(UserName like '%' + @Keyword + '%' or DisplayName like '%' + @Keyword + '%')");
        }
        builder.AddOrderBy("Id desc");
        builder.AddPageSize(searchModel.PageIndex, searchModel.PageSize);
        return SelectList(builder.SQL, builder.GetDynamicParameters(searchModel)).ToList();
    }

    #endregion
}
