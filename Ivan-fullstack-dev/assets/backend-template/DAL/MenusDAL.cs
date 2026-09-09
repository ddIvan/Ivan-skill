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
/// Menus操作类
/// </summary>
public partial class MenusDAL
    : BaseDAL<Menus>, IMenusDAL
{
    public MenusDAL() : base()
    {
    }

    public MenusDAL(IDbHelper dbHelper) : base(dbHelper)
    {
    }

    protected override string InsertSql => @"INSERT into Menus (Name, Path, Icon, ParentId, Sort, IsVisible, CreateTime)
                             VALUES (@Name, @Path, @Icon, @ParentId, @Sort, @IsVisible, @CreateTime)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from Menus WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE Menus SET Name=@Name, Path=@Path, Icon=@Icon, ParentId=@ParentId, 
                                    Sort=@Sort, IsVisible=@IsVisible
                                    where Id = @Id";

    protected override string SelectAllSql => @"select * from Menus (nolock) order by Sort";

    #region 查询

    public override Menus Select(Menus value)
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE Id = @Id";
        return SelectFirst(sql, value);
    }

    /// <summary>按路由路径精准查询</summary>
    public Menus? GetByPath(string path)
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE Path = @Path";
        return SelectFirst(sql, new { Path = path });
    }

    #endregion
}
