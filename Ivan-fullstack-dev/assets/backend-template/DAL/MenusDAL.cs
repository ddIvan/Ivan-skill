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
/// Menus 数据访问类 — 支持无限级嵌套菜单（邻接表 + 路径枚举）
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

    protected override string InsertSql => @"INSERT into Menus (Name, MenuType, Path, Icon, ParentId, FullPath, Level, Sort, IsVisible, IsEnabled, PermissionCode, ControllerAction, Remark, CreateTime)
                             VALUES (@Name, @MenuType, @Path, @Icon, @ParentId, @FullPath, @Level, @Sort, @IsVisible, @IsEnabled, @PermissionCode, @ControllerAction, @Remark, @CreateTime)";

    protected override string InsertSqlForGeneratedKey => InsertSql + ";select SCOPE_IDENTITY();";

    protected override string DeleteSql => @"DELETE from Menus WHERE Id = @Id";

    protected override string UpdateSql => @"UPDATE Menus SET Name=@Name, MenuType=@MenuType, Path=@Path, Icon=@Icon, ParentId=@ParentId,
                                    FullPath=@FullPath, Level=@Level, Sort=@Sort, IsVisible=@IsVisible, IsEnabled=@IsEnabled,
                                    PermissionCode=@PermissionCode, ControllerAction=@ControllerAction, Remark=@Remark
                                    where Id = @Id";

    protected override string SelectAllSql => @"select * from Menus (nolock) order by Level, Sort";

    #region 查询

    public override Menus Select(Menus value)
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE Id = @Id";
        // 注意：必须用 DbHelper.Query（自动携带会话事务），直接用 DbHelper.Connection.Query
        // 会在事务内调用时报 "command has no transaction" 错误
        return DbHelper.Query<Menus>(sql, value).FirstOrDefault();
    }

    /// <summary>按路由路径精准查询（仅匹配菜单节点 MenuType=2）</summary>
    public Menus? GetByPath(string path)
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE Path = @Path AND MenuType = 2";
        return DbHelper.Query<Menus>(sql, new { Path = path }).FirstOrDefault();
    }

    /// <summary>按 FullPath 精准查询</summary>
    public Menus? GetByFullPath(string fullPath)
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE FullPath = @FullPath";
        return DbHelper.Query<Menus>(sql, new { FullPath = fullPath }).FirstOrDefault();
    }

    /// <summary>查询指定节点的所有子孙节点（FullPath LIKE 前缀匹配，利用 IX_Menus_FullPath 索引）</summary>
    public List<Menus> GetDescendants(string fullPath)
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE FullPath LIKE @Pattern + '%' AND Id != (SELECT TOP 1 Id FROM Menus (nolock) WHERE FullPath = @FullPath) ORDER BY Level, Sort";
        return DbHelper.Query<Menus>(sql, new { Pattern = fullPath + "/", FullPath = fullPath }).ToList();
    }

    /// <summary>查询指定父节点的直接子节点（按 Sort 排序）</summary>
    public List<Menus> GetChildren(int parentId)
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE ParentId = @ParentId ORDER BY Sort";
        return DbHelper.Query<Menus>(sql, new { ParentId = parentId }).ToList();
    }

    /// <summary>查询指定父节点下的最大排序号（新节点默认 Sort = Max + 1）</summary>
    public int GetMaxSort(int? parentId)
    {
        if (parentId.HasValue)
        {
            string sql = "SELECT ISNULL(MAX(Sort), 0) FROM Menus (nolock) WHERE ParentId = @ParentId";
            return DbHelper.ExecuteScalar<int>(sql, new { ParentId = parentId.Value });
        }
        else
        {
            string sql = "SELECT ISNULL(MAX(Sort), 0) FROM Menus (nolock) WHERE ParentId IS NULL";
            return DbHelper.ExecuteScalar<int>(sql);
        }
    }

    /// <summary>获取所有一级节点（按 Sort 排序）</summary>
    public List<Menus> GetRootMenus()
    {
        string sql = "SELECT * FROM Menus (nolock) WHERE ParentId IS NULL ORDER BY Sort";
        return DbHelper.Query<Menus>(sql).ToList();
    }

    /// <summary>查询是否存在子节点</summary>
    public bool HasChildren(int parentId)
    {
        string sql = "SELECT COUNT(1) FROM Menus (nolock) WHERE ParentId = @ParentId";
        return DbHelper.ExecuteScalar<int>(sql, new { ParentId = parentId }) > 0;
    }

    #endregion

    #region 批量更新

    /// <summary>批量更新指定子树节点的 FullPath 和 Level（节点移动时级联更新，事务中调用）</summary>
    public void BatchUpdateFullPath(List<(int Id, string FullPath, int Level)> updates)
    {
        // DbHelper.Execute 自动携带会话事务
        foreach (var (id, fullPath, level) in updates)
        {
            DbHelper.Execute(
                "UPDATE Menus SET FullPath = @FullPath, Level = @Level WHERE Id = @Id",
                new { Id = id, FullPath = fullPath, Level = level });
        }
    }

    /// <summary>批量更新同级节点的排序号（传入 (Id, Sort) 列表）</summary>
    public void BatchUpdateSort(List<(int Id, int Sort)> updates)
    {
        foreach (var (id, sort) in updates)
        {
            DbHelper.Execute(
                "UPDATE Menus SET Sort = @Sort WHERE Id = @Id",
                new { Id = id, Sort = sort });
        }
    }

    #endregion
}