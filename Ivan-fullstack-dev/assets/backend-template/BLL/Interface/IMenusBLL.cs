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
/// Menus操作接口
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IMenusBLL : IBaseBLL<Menus>
{
    List<Menus> GetAll();
    List<Menus> GetTree();
    Menus? GetById(int id);
    Menus? GetByPath(string path);
    ResultModel<int> Create(Menus model);
    ResultModel Modify(Menus model);
    ResultModel Remove(int id);
}
