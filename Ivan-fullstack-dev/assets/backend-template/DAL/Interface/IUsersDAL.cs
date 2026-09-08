using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;

namespace IvanProject.DAL.Interface;

/// <summary>
/// Users数据层接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IUsersDAL : IBaseDAL<Users>
{
    List<Users> Search(PageSearchModel searchModel, out int totalRecordCount);
}
