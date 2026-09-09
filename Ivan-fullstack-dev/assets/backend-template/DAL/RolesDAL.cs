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
/// Roles操作类
/// </summary>
public partial class RolesDAL
    : BaseDAL<Roles>, IRolesDAL
{
    public RolesDAL() : base()
    {
    }

    public RolesDAL(IDbHelper dbHelper) : base(dbHelper)
    {
    }

    protected override string InsertSql => @"INSERT into Roles (Name, Code, Description, CreateTime)
                             VALUES (@Name, @Code, @Description, @CreateTime)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from Roles WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE Roles SET Name=@Name, Code=@Code, Description=@Description
                                    where Id = @Id";

    protected override string SelectAllSql => @"select * from Roles (nolock)";

    #region 查询

    public override Roles Select(Roles value)
    {
        string sql = "SELECT * FROM Roles (nolock) WHERE Id = @Id";
        return SelectFirst(sql, value);
    }

    /// <summary>按编码精准查询（SQL 层 WHERE，禁止全表加载后内存过滤）</summary>
    public Roles? GetByCode(string code)
    {
        string sql = "SELECT * FROM Roles (nolock) WHERE Code = @Code";
        return SelectFirst(sql, new { Code = code });
    }

    /// <summary>分页搜索（SQL 层分页，OFFSET/FETCH）</summary>
    public List<Roles> Search(PageSearchModel searchModel, out int totalRecordCount)
    {
        var builder = new SQLBuilder("select count(1) from Roles (nolock)", SqlType);
        if (!string.IsNullOrWhiteSpace(searchModel.Keyword))
        {
            builder.AddWhere("(Name like '%' + @Keyword + '%' or Code like '%' + @Keyword + '%')");
        }
        totalRecordCount = CountBySql(builder.SQL, builder.GetDynamicParameters(searchModel));

        builder = new SQLBuilder("select * from Roles (nolock)", SqlType);
        if (!string.IsNullOrWhiteSpace(searchModel.Keyword))
        {
            builder.AddWhere("(Name like '%' + @Keyword + '%' or Code like '%' + @Keyword + '%')");
        }
        builder.AddOrderBy("Id desc");
        builder.AddPageSize(searchModel.PageIndex, searchModel.PageSize);
        return SelectList(builder.SQL, builder.GetDynamicParameters(searchModel)).ToList();
    }

    #endregion
}
