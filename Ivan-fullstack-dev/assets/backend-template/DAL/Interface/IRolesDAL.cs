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
/// Roles数据层接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IRolesDAL : IBaseDAL<Roles>
{
    List<Roles> Search(PageSearchModel searchModel, out int totalRecordCount);

    /// <summary>按编码精准查询</summary>
    Roles? GetByCode(string code);
}
