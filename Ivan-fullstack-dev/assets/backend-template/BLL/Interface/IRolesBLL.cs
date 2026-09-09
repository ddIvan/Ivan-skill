using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;

namespace IvanProject.BLL.Interface;

/// <summary>
/// Roles操作接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IRolesBLL : IBaseBLL<Roles>
{
    PageResultModel<List<Roles>> List(PageSearchModel searchModel);
    List<Roles> GetALL();
    ResultModel<int> Create(Roles model);
    Roles? GetById(int id);
    Roles? GetByCode(string code);
    ResultModel Modify(Roles model);
    ResultModel Remove(int id);
}
