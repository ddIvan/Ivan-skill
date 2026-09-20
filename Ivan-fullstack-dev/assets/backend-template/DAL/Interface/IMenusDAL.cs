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
/// Menus数据层接口 — 无限级嵌套菜单
/// </summary>
[Ivan.IOC.Core.Attributes.InjectIOC]
public interface IMenusDAL : IBaseDAL<Menus>
{
    /// <summary>按路由路径精准查询</summary>
    Menus? GetByPath(string path);

    /// <summary>查询指定节点的所有子孙节点（通过 FullPath LIKE 前缀匹配）</summary>
    List<Menus> GetDescendants(string fullPath);

    /// <summary>查询指定父节点的直接子节点（按 Sort 排序）</summary>
    List<Menus> GetChildren(int parentId);

    /// <summary>查询指定父节点下的最大排序号</summary>
    int GetMaxSort(int? parentId);

    /// <summary>批量更新指定子树节点的 FullPath（节点移动时级联更新）</summary>
    void BatchUpdateFullPath(List<(int Id, string FullPath, int Level)> updates);

    /// <summary>批量更新同级节点的排序号（拖拽排序）</summary>
    void BatchUpdateSort(List<(int Id, int Sort)> updates);

    /// <summary>按 FullPath 精准查询</summary>
    Menus? GetByFullPath(string fullPath);

    /// <summary>获取所有一级节点（按 Sort 排序）</summary>
    List<Menus> GetRootMenus();

    /// <summary>查询是否存在子节点</summary>
    bool HasChildren(int parentId);
}
