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
/// Users操作类（标准 CRUD + 分页 + 搜索模式，新业务模块参照此写法）
/// </summary>
public partial class UsersBLL
    : BaseBLL<IUsersDAL, Users>, IUsersBLL
{
    public UsersBLL(IDataSessionFactory factory) : base(factory)
    {
    }

    /// <summary>全量查询（会话内创建 DAL 执行）</summary>
    public List<Users> GetALL()
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IUsersDAL>().SelectAll().ToList();
        }
    }

    /// <summary>分页查询（内部调用 DAL.Search，支持 keyword 过滤）</summary>
    public PageResultModel<List<Users>> List(PageSearchModel searchModel)
    {
        var result = new PageResultModel<List<Users>>();
        using (var session = Factory.OpenSession())
        {
            var list = session.CreateDAL<IUsersDAL>().Search(searchModel, out int total);
            result.Data = list;
            result.TotalRecord = total;
        }
        return result;
    }

    /// <summary>新增用户（自增主键）</summary>
    public ResultModel<int> Create(Users model)
    {
        using (var session = Factory.OpenSession())
        {
            var id = session.CreateDAL<IUsersDAL>().InsertForGeneratedKey(model);
            // 注意：ResultModel 默认 Success=false，必须用 BuildSuccess 显式标记成功，
            // 否则调用方 !result.Success 会误判为失败（抛出空消息的 BusinessException）
            return ResultModel<int>.BuildSuccess(id);
        }
    }

    /// <summary>按主键查询</summary>
    public Users? GetById(int id)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IUsersDAL>().Select(new Users { Id = id });
        }
    }

    /// <summary>按用户名查询（用于唯一性校验）</summary>
    public Users? GetByUserName(string userName)
    {
        var lower = userName?.ToLower();
        return GetALL().FirstOrDefault(u => u.UserName.ToLower() == lower);
    }

    /// <summary>修改用户</summary>
    public ResultModel Modify(Users model)
    {
        using (var session = Factory.OpenSession())
        {
            session.CreateDAL<IUsersDAL>().Update(model);
        }
        // 默认 ResultModel 的 Success=false，这里必须显式 BuildSuccess
        return ResultModel.BuildSuccess();
    }

    /// <summary>删除用户</summary>
    public ResultModel Remove(int id)
    {
        using (var session = Factory.OpenSession())
        {
            session.CreateDAL<IUsersDAL>().Delete(new Users { Id = id });
        }
        return ResultModel.BuildSuccess();
    }
}
