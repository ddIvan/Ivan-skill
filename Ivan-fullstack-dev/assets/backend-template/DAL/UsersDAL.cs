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

    /// <summary>
    /// 分页搜索（示例：全量查询后内存分页，大数据量表请改写为 SQL 分页）
    /// </summary>
    public List<Users> Search(PageSearchModel searchModel, out int totalRecordCount)
    {
        var list = SelectAll();
        totalRecordCount = list.Count;
        return list;
    }

    #endregion
}
