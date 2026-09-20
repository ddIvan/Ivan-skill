using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Models;
using IvanProject.DTOs;

namespace IvanProject.BLL.Interface;

/// <summary>
/// 菜单业务接口 — 无限级嵌套管理
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IMenusBLL : IBaseBLL<Menus>
{
    List<Menus> GetAll();
    List<Menus> GetTree();
    List<MenuTreeNode> GetTreeForFrontend(int? userId = null);
    List<MenuTreeNode> GetChildren(int parentId);
    Menus? GetById(int id);
    Menus? GetByPath(string path);
    Menus? GetByFullPath(string fullPath);
    List<Menus> GetDescendants(int nodeId);
    ResultModel<int> Create(Menus model);
    ResultModel Modify(Menus model);
    ResultModel SetEnabled(int id, bool isEnabled);
    ResultModel Remove(int id);
    ResultModel Reorder(MenuSortRequest request);
}
