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
/// Users操作类（BaseBLL 提供 Add/Select/Update/Delete 等通用方法）
/// </summary>
public partial class UsersBLL
    : BaseBLL<IUsersDAL, Users>, IUsersBLL
{
    public UsersBLL(IDataSessionFactory factory) : base(factory)
    {
    }

    /// <summary>通过会话工厂创建 DAL 执行查询的标准写法</summary>
    public List<Users> GetALL()
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IUsersDAL>().SelectAll().ToList();
        }
    }

    /// <summary>分页查询（内部调用 DAL.Search）</summary>
    public PageResultModel<List<Users>> List(PageSearchModel searchModel)
    {
        var result = new PageResultModel<List<Users>>();
        using (var session = Factory.OpenSession())
        {
            var list = session.CreateDAL<IUsersDAL>().Search(searchModel, out int total);
            result.Data = list;
            result.Count = total;
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
}
