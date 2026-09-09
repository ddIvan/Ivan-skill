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
/// Roles操作类（标准 CRUD + 分页）
/// </summary>
public partial class RolesBLL
    : BaseBLL<IRolesDAL, Roles>, IRolesBLL
{
    public RolesBLL(IDataSessionFactory factory) : base(factory)
    {
    }

    /// <summary>全量查询</summary>
    public List<Roles> GetALL()
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IRolesDAL>().SelectAll().ToList();
        }
    }

    /// <summary>分页查询</summary>
    public PageResultModel<List<Roles>> List(PageSearchModel searchModel)
    {
        var result = new PageResultModel<List<Roles>>();
        using (var session = Factory.OpenSession())
        {
            var list = session.CreateDAL<IRolesDAL>().Search(searchModel, out int total);
            result.Data = list;
            result.TotalRecord = total;
        }
        return result;
    }

    /// <summary>新增角色</summary>
    public ResultModel<int> Create(Roles model)
    {
        using (var session = Factory.OpenSession())
        {
            model.CreateTime = DateTime.Now;
            var id = session.CreateDAL<IRolesDAL>().InsertForGeneratedKey(model);
            return ResultModel<int>.BuildSuccess(id);
        }
    }

    /// <summary>按主键查询</summary>
    public Roles? GetById(int id)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IRolesDAL>().Select(new Roles { Id = id });
        }
    }

    /// <summary>按编码精准查询（走 SQL 层 WHERE）</summary>
    public Roles? GetByCode(string code)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IRolesDAL>().GetByCode(code);
        }
    }

    /// <summary>修改角色</summary>
    public ResultModel Modify(Roles model)
    {
        using (var session = Factory.OpenSession())
        {
            session.CreateDAL<IRolesDAL>().Update(model);
        }
        return ResultModel.BuildSuccess();
    }

    /// <summary>删除角色</summary>
    public ResultModel Remove(int id)
    {
        using (var session = Factory.OpenSession())
        {
            session.CreateDAL<IRolesDAL>().Delete(new Roles { Id = id });
        }
        return ResultModel.BuildSuccess();
    }
}
