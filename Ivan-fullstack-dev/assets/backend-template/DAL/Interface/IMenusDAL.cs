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
/// Menus数据层接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IMenusDAL : IBaseDAL<Menus>
{
    /// <summary>按路由路径精准查询</summary>
    Menus? GetByPath(string path);
}
