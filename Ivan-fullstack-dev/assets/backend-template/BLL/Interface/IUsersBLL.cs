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
/// Users操作接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IUsersBLL : IBaseBLL<Users>
{
    PageResultModel<List<Users>> List(PageSearchModel searchModel);
    List<Users> GetALL();
    ResultModel<int> Create(Users model);
}
