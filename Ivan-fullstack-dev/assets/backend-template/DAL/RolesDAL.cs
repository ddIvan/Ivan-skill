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
        return DbHelper.Connection.Query<Roles>(sql, value).FirstOrDefault();
    }

    /// <summary>按编码精准查询（SQL 层 WHERE，禁止全表加载后内存过滤）</summary>
    public Roles? GetByCode(string code)
    {
        string sql = "SELECT * FROM Roles (nolock) WHERE Code = @Code";
        return DbHelper.Connection.Query<Roles>(sql, new { Code = code }).FirstOrDefault();
    }

    /// <summary>分页搜索（SQL 层过滤 + OFFSET/FETCH 分页）</summary>
    public List<Roles> Search(PageSearchModel searchModel, out int totalRecordCount)
    {
        string where = "1=1";
        object? param = null;
        var keyword = searchModel.TryGetModelValue("keyword", out var kw) ? kw : null;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            where = "(Name like '%' + @Keyword + '%' or Code like '%' + @Keyword + '%')";
            param = new { Keyword = keyword };
        }

        string countSql = $"select count(1) from Roles (nolock) where {where}";
        string dataSql = $"select * from Roles (nolock) where {where}";

        // SelectByPage 的页码为 0-based（MsSql rownumber>page*size / MySQL limit page*size），
        // PageSearchModel.Page 为 1-based（从 1 开始），此处必须 -1，否则首页会跳过前 N 条数据
        var page = SelectByPage<Roles>(dataSql, searchModel.Page - 1, searchModel.Limit, out totalRecordCount, param, "Id desc");
        return page.ToList();
    }

    #endregion
}
