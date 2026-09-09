using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;
using IvanProject.DAL;
using IvanProject.DAL.Interface;
using IvanProject.BLL.Interface;

namespace IvanProject.BLL;

/// <summary>
/// Menus操作类（标准 CRUD + 树形结构）
/// </summary>
public partial class MenusBLL
    : BaseBLL<IMenusDAL, Menus>, IMenusBLL
{
    public MenusBLL(IDataSessionFactory factory) : base(factory)
    {
    }

    /// <summary>全量查询（按 Sort 排序）</summary>
    public List<Menus> GetAll()
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IMenusDAL>().SelectAll().ToList();
        }
    }

    /// <summary>获取菜单树（一级菜单包含 children）</summary>
    public List<Menus> GetTree()
    {
        var all = GetAll();
        var lookup = all.ToDictionary(m => m.Id);
        var roots = new List<Menus>();
        foreach (var menu in all.OrderBy(m => m.Sort))
        {
            if (menu.ParentId == null || !lookup.ContainsKey(menu.ParentId.Value))
            {
                roots.Add(menu);
            }
            else
            {
                lookup[menu.ParentId.Value].Children.Add(menu);
            }
        }
        return roots.OrderBy(m => m.Sort).ToList();
    }

    /// <summary>按主键查询</summary>
    public Menus? GetById(int id)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IMenusDAL>().Select(new Menus { Id = id });
        }
    }

    /// <summary>按路由路径精准查询（走 SQL 层 WHERE）</summary>
    public Menus? GetByPath(string path)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IMenusDAL>().GetByPath(path);
        }
    }

    /// <summary>新增菜单</summary>
    public ResultModel<int> Create(Menus model)
    {
        using (var session = Factory.OpenSession())
        {
            model.CreateTime = DateTime.Now;
            var id = session.CreateDAL<IMenusDAL>().InsertForGeneratedKey(model);
            return ResultModel<int>.BuildSuccess(id);
        }
    }

    /// <summary>修改菜单</summary>
    public ResultModel Modify(Menus model)
    {
        using (var session = Factory.OpenSession())
        {
            session.CreateDAL<IMenusDAL>().Update(model);
        }
        return ResultModel.BuildSuccess();
    }

    /// <summary>删除菜单（含子菜单）</summary>
    public ResultModel Remove(int id)
    {
        using (var session = Factory.OpenSession())
        {
            session.CreateDAL<IMenusDAL>().Delete(new Menus { Id = id });
        }
        return ResultModel.BuildSuccess();
    }
}
