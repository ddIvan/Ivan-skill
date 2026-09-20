using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;
using Dapper;
using Ivan.Data;
using Ivan.Data.IOC;
using Ivan.Common;
using IvanProject.Common;
using IvanProject.Models;
using IvanProject.DAL;
using IvanProject.DAL.Interface;
using IvanProject.BLL.Interface;
using IvanProject.DTOs;

namespace IvanProject.BLL;

/// <summary>
/// 菜单业务逻辑 — 统一权限树（目录/菜单/按钮三类节点）。
/// 
/// 核心操作：
///   - CRUD：增删改查（含按路径/FullPath查询）
///   - 树构建：全量加载 → 内存构建树（O(n) 哈希表法）
///   - 排序/拖拽：同级节点 Sort 重新编排 + 跨父级移动时级联更新 FullPath
///   - 启用/禁用：递归影响子孙节点
///   - 校验：按钮节点 PermissionCode 必填且全局唯一（两段式）、ControllerAction 必须存在于扫描结果
///   - 缓存：MenuCacheService（树/列表/单节点）+ RolePermissionService（权限编码），配置变更后级联失效
/// </summary>
public partial class MenusBLL
    : BaseBLL<IMenusDAL, Menus>, IMenusBLL
{
    private readonly MenuCacheService _cache;
    private readonly ICacheService _globalCache;

    public MenusBLL(IDataSessionFactory factory, MenuCacheService cache, ICacheService globalCache) : base(factory)
    {
        _cache = cache;
        _globalCache = globalCache;
    }

    // ==================== 查询 ====================

    /// <summary>全量查询（按 Level + Sort 排序），优先缓存</summary>
    public List<Menus> GetAll()
    {
        var cached = _cache.GetAll();
        if (cached != null) return cached;

        using (var session = Factory.OpenSession())
        {
            var all = session.CreateDAL<IMenusDAL>().SelectAll().ToList();
            _cache.SetAll(all);
            return all;
        }
    }

    /// <summary>
    /// 获取菜单树（顶层节点含递归 Children）。
    /// 算法：O(n) 哈希表法，一次全量查询 + 单次遍历构建。
    /// </summary>
    public List<Menus> GetTree()
    {
        var all = GetAll();
        return BuildTree(all);
    }

    /// <summary>
    /// 获取用于前端渲染的菜单树（DTO，含 HasChildren 标记用于懒加载）。
    /// 仅返回启用且可见的节点；按钮节点（MenuType=3）不进入侧边栏。
    /// </summary>
    public List<MenuTreeNode> GetTreeForFrontend(int? userId = null)
    {
        var all = GetAll().Where(m => m.IsEnabled != false && m.IsVisible != false && m.MenuType != 3).ToList();
        return BuildTreeNodeTree(all, userId);
    }

    /// <summary>
    /// 懒加载：获取指定节点的直接子节点。
    /// 前端展开节点时按需加载，避免一次性传输整棵大树。
    /// </summary>
    public List<MenuTreeNode> GetChildren(int parentId)
    {
        using (var session = Factory.OpenSession())
        {
            var dal = session.CreateDAL<IMenusDAL>();
            var children = dal.GetChildren(parentId);
            var result = new List<MenuTreeNode>();
            foreach (var child in children)
            {
                result.Add(new MenuTreeNode
                {
                    Id = child.Id,
                    Name = child.Name,
                    MenuType = child.MenuType,
                    Path = child.Path,
                    Icon = child.Icon,
                    ParentId = child.ParentId,
                    FullPath = child.FullPath,
                    Level = child.Level,
                    Sort = child.Sort,
                    IsVisible = child.IsVisible != false,
                    IsEnabled = child.IsEnabled != false,
                    PermissionCode = child.PermissionCode,
                    ControllerAction = child.ControllerAction,
                    Remark = child.Remark,
                    HasChildren = dal.HasChildren(child.Id),
                    Children = new List<MenuTreeNode>()
                });
            }
            return result;
        }
    }

    /// <summary>按主键查询</summary>
    public Menus? GetById(int id)
    {
        var cached = _cache.GetNode(id);
        if (cached != null) return cached;

        using (var session = Factory.OpenSession())
        {
            var menu = session.CreateDAL<IMenusDAL>().Select(new Menus { Id = id });
            if (menu != null) _cache.SetNode(menu);
            return menu;
        }
    }

    /// <summary>按路由路径精准查询（走 SQL 层 WHERE）</summary>
    public Menus? GetByPath(string path)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IMenusDAL>().GetByPath(path);
        }
    }

    /// <summary>按 FullPath（祖先路径）精准查询</summary>
    public Menus? GetByFullPath(string fullPath)
    {
        using (var session = Factory.OpenSession())
        {
            return session.CreateDAL<IMenusDAL>().GetByFullPath(fullPath);
        }
    }

    /// <summary>
    /// 查询某节点的所有子孙节点（包含自身）
    /// 使用 FullPath LIKE 前缀匹配，一条 SQL 解决，索引加速。
    /// </summary>
    public List<Menus> GetDescendants(int nodeId)
    {
        var node = GetById(nodeId);
        if (node == null || string.IsNullOrEmpty(node.FullPath))
            return new List<Menus>();

        using (var session = Factory.OpenSession())
        {
            var descendants = session.CreateDAL<IMenusDAL>().GetDescendants(node.FullPath);
            // GetDescendants 不包含自身，手动加入
            descendants.Insert(0, node);
            return descendants;
        }
    }

    // ==================== 新增 ====================

    /// <summary>新增菜单节点。自动计算 FullPath、Level 和 Sort。</summary>
    public ResultModel<int> Create(Menus model)
    {
        using (var session = Factory.OpenSession())
        {
            session.BeginTrans();
            try
            {
                var dal = session.CreateDAL<IMenusDAL>();

                // 默认 Sort = 同级最大 + 1
                if (model.Sort <= 0)
                    model.Sort = dal.GetMaxSort(model.ParentId) + 1;

                // 计算 Level
                if (model.ParentId.HasValue && model.ParentId.Value > 0)
                {
                    var parent = dal.Select(new Menus { Id = model.ParentId.Value });
                    if (parent == null)
                        return ResultModel<int>.BuildFail(1, "父节点不存在");

                    model.Level = parent.Level + 1;
                }
                else
                {
                    model.ParentId = null;
                    model.Level = 0;
                }

                // 统一权限树校验：按钮节点必须配置唯一的两段式权限编码，绑定的后端接口必须存在
                var permCheck = ValidatePermissionConfig(dal, model);
                if (!permCheck.Success)
                    return permCheck;

                model.IsVisible ??= true;
                model.IsEnabled ??= true;
                model.CreateTime = DateTime.Now;

                var id = dal.InsertForGeneratedKey(model);

                // 插入后计算 FullPath（需要拿到自增ID）
                model.Id = id;
                if (model.ParentId.HasValue && model.ParentId.Value > 0)
                {
                    var parent = dal.Select(new Menus { Id = model.ParentId.Value });
                    model.FullPath = $"{parent!.FullPath}/{id}";
                }
                else
                {
                    model.FullPath = $"/{id}";
                }

                // 更新 FullPath 回数据库
                dal.Update(model);
                session.CommitTrans();

                // 缓存失效（菜单 + 角色权限编码）
                _cache.InvalidateAll();
                ClearPermissionCaches();

                return ResultModel<int>.BuildSuccess(id);
            }
            catch
            {
                session.RollbackTrans();
                throw;
            }
        }
    }

    // ==================== 更新 ====================

    /// <summary>修改菜单基本信息（名称/路径/图标/可见/启用）</summary>
    public ResultModel Modify(Menus model)
    {
        using (var session = Factory.OpenSession())
        {
            session.BeginTrans();
            try
            {
                var dal = session.CreateDAL<IMenusDAL>();
                var existing = dal.Select(new Menus { Id = model.Id });
                if (existing == null)
                    return ResultModel.BuildFail(1, "菜单不存在");

                // 仅更新允许的字段（不改变 ParentId/FullPath/Level）
                existing.Name = model.Name;
                existing.MenuType = model.MenuType;
                existing.Path = model.Path;
                existing.Icon = model.Icon;
                existing.IsVisible = model.IsVisible;
                existing.IsEnabled = model.IsEnabled;
                existing.PermissionCode = model.PermissionCode;
                existing.ControllerAction = model.ControllerAction;
                existing.Remark = model.Remark;
                // 如果传入 Sort，也更新（常规编辑允许直接改排序号）
                if (model.Sort > 0)
                    existing.Sort = model.Sort;

                // 统一权限树校验（排除自身）
                var permCheck = ValidatePermissionConfig(dal, existing, excludeId: model.Id);
                if (!permCheck.Success)
                    return permCheck;

                dal.Update(existing);
                session.CommitTrans();

                _cache.InvalidateAll();
                _cache.ClearNode(model.Id);
                ClearPermissionCaches();

                return ResultModel.BuildSuccess();
            }
            catch
            {
                session.RollbackTrans();
                throw;
            }
        }
    }

    /// <summary>启用/禁用菜单节点。禁用后递归影响子孙节点在菜单树的显示。</summary>
    public ResultModel SetEnabled(int id, bool isEnabled)
    {
        using (var session = Factory.OpenSession())
        {
            session.BeginTrans();
            try
            {
                var dal = session.CreateDAL<IMenusDAL>();
                var node = dal.Select(new Menus { Id = id });
                if (node == null)
                    return ResultModel.BuildFail(1, "菜单不存在");

                node.IsEnabled = isEnabled;
                dal.Update(node);
                session.CommitTrans();

                _cache.InvalidateAll();
                _cache.ClearNode(id);
                ClearPermissionCaches();

                return ResultModel.BuildSuccess();
            }
            catch
            {
                session.RollbackTrans();
                throw;
            }
        }
    }

    // ==================== 删除 ====================

    /// <summary>
    /// 删除菜单节点（含所有子孙节点）。
    /// 使用 FullPath LIKE 前缀匹配获取所有子孙 ID，事务中一并删除。
    /// </summary>
    public ResultModel Remove(int id)
    {
        using (var session = Factory.OpenSession())
        {
            session.BeginTrans();
            try
            {
                var dal = session.CreateDAL<IMenusDAL>();
                var node = dal.Select(new Menus { Id = id });
                if (node == null)
                    return ResultModel.BuildFail(1, "菜单不存在");

                // 获取所有子孙节点（不含自身）
                var descendants = dal.GetDescendants(node.FullPath!);

                // 先删关联数据：RoleMenus（统一树模型下唯一的授权关联表）
                // 注意：通过 DAL 的 DbHelper 连接直接执行（与事务共用同一连接）
                if (descendants.Any())
                {
                    var allIds = descendants.Select(d => d.Id).Append(id).ToList();
                    foreach (var menuId in allIds)
                    {
                        dal.DbHelper.Connection.Execute("DELETE FROM RoleMenus WHERE MenuId = @MenuId", new { MenuId = menuId });
                    }
                }

                // 从底层向上删除子孙节点（避免外键问题）
                foreach (var desc in descendants.OrderByDescending(d => d.Level))
                {
                    dal.Delete(new Menus { Id = desc.Id });
                }
                // 最后删除自身
                dal.Delete(new Menus { Id = id });

                session.CommitTrans();

                // 全量缓存失效
                _cache.InvalidateAll();

                return ResultModel.BuildSuccess();
            }
            catch
            {
                session.RollbackTrans();
                throw;
            }
        }
    }

    // ==================== 排序/拖拽 ====================

    /// <summary>
    /// 拖拽排序：将节点移动到指定位置（同父级内或跨父级移动）。
    /// 
    /// 逻辑：
    ///   1. 若 TargetParentId 变化 → 跨父级移动，需级联更新 FullPath
    ///   2. 同级内重排 Sort：将目标节点插入到 BeforeId 之前或 AfterId 之后
    ///   3. 事务保证原子性
    /// </summary>
    public ResultModel Reorder(MenuSortRequest request)
    {
        using (var session = Factory.OpenSession())
        {
            session.BeginTrans();
            try
            {
                var dal = session.CreateDAL<IMenusDAL>();
                var node = dal.Select(new Menus { Id = request.Id });
                if (node == null)
                    return ResultModel.BuildFail(1, "菜单不存在");

                var oldParentId = node.ParentId;
                var newParentId = request.TargetParentId;

                // 1. 计算新的排序位置
                int newSort;
                var siblings = newParentId.HasValue
                    ? dal.GetChildren(newParentId.Value)
                    : dal.GetRootMenus();

                // 移除自己（如果原来就在同级中）
                siblings = siblings.Where(s => s.Id != request.Id).ToList();

                if (request.BeforeId.HasValue)
                {
                    var beforeNode = siblings.FirstOrDefault(s => s.Id == request.BeforeId.Value);
                    if (beforeNode == null)
                        return ResultModel.BuildFail(1, "目标位置节点不存在");

                    newSort = beforeNode.Sort;
                    // 将 beforeNode 及其后面的节点 Sort + 1
                    foreach (var s in siblings.Where(s => s.Sort >= newSort).OrderByDescending(s => s.Sort))
                    {
                        s.Sort++;
                        dal.Update(s);
                    }
                }
                else if (request.AfterId.HasValue)
                {
                    var afterNode = siblings.FirstOrDefault(s => s.Id == request.AfterId.Value);
                    if (afterNode == null)
                        return ResultModel.BuildFail(1, "目标位置节点不存在");

                    newSort = afterNode.Sort + 1;
                    // 将 afterNode 后面的节点 Sort + 1
                    foreach (var s in siblings.Where(s => s.Sort > afterNode.Sort).OrderByDescending(s => s.Sort))
                    {
                        s.Sort++;
                        dal.Update(s);
                    }
                }
                else
                {
                    // 放到同级末尾
                    newSort = siblings.Any() ? siblings.Max(s => s.Sort) + 1 : 1;
                }

                // 2. 更新节点
                node.ParentId = newParentId;
                node.Sort = newSort;

                // 3. 若发生跨父级移动，级联更新 FullPath 和 Level
                if (oldParentId != newParentId)
                {
                    UpdateNodePathAndDescendants(dal, node, newParentId);
                }
                else
                {
                    dal.Update(node);
                }

                session.CommitTrans();
                _cache.InvalidateAll();

                return ResultModel.BuildSuccess();
            }
            catch
            {
                session.RollbackTrans();
                throw;
            }
        }
    }

    /// <summary>
    /// 节点移动后级联更新 FullPath 和 Level：
    ///   1. 计算新 FullPath = 父节点.FullPath + "/" + node.Id
    ///   2. 获取所有子孙节点，替换旧 FullPath 前缀为新 FullPath
    ///   3. 更新所有受影响节点的 Level = 新Level + 原偏移
    /// </summary>
    private void UpdateNodePathAndDescendants(IMenusDAL dal, Menus node, int? newParentId)
    {
        var oldFullPath = node.FullPath!;
        var oldLevel = node.Level;

        // 计算新的 FullPath 和 Level
        string newFullPath;
        int newLevel;
        if (newParentId.HasValue && newParentId.Value > 0)
        {
            var parent = dal.Select(new Menus { Id = newParentId.Value });
            if (parent == null) throw new InvalidOperationException("父节点不存在");
            newFullPath = $"{parent.FullPath}/{node.Id}";
            newLevel = parent.Level + 1;
        }
        else
        {
            newFullPath = $"/{node.Id}";
            newLevel = 0;
        }

        var levelOffset = newLevel - oldLevel;

        // 获取所有子孙节点（含自身）
        var allNodes = dal.GetDescendants(oldFullPath);
        allNodes.Insert(0, node);

        // 级联更新
        foreach (var n in allNodes)
        {
            n.FullPath = n.FullPath!.Replace(oldFullPath, newFullPath);
            n.Level += levelOffset;
            dal.Update(n);
        }
    }

    // ==================== 树构建辅助 ====================

    /// <summary>O(n) 哈希表法构建菜单树</summary>
    private List<Menus> BuildTree(List<Menus> all)
    {
        var lookup = all.ToDictionary(m => m.Id);
        var roots = new List<Menus>();

        foreach (var menu in all.OrderBy(m => m.Level).ThenBy(m => m.Sort))
        {
            menu.Children = new List<Menus>();

            if (menu.ParentId == null || !lookup.ContainsKey(menu.ParentId.Value))
            {
                roots.Add(menu);
            }
            else
            {
                lookup[menu.ParentId.Value].Children.Add(menu);
            }
        }

        // 递归排序每层的 Children
        SortChildrenRecursive(roots);

        return roots.OrderBy(m => m.Sort).ToList();
    }

    private void SortChildrenRecursive(List<Menus> nodes)
    {
        foreach (var node in nodes)
        {
            node.Children = node.Children.OrderBy(c => c.Sort).ToList();
            SortChildrenRecursive(node.Children);
        }
    }

    /// <summary>构建前端 MenuTreeNode DTO 树（子节点递归挂载到父节点 Children）</summary>
    private List<MenuTreeNode> BuildTreeNodeTree(List<Menus> all, int? userId = null)
    {
        var lookup = all.ToDictionary(m => m.Id);

        // 第一遍：为每个节点创建 DTO
        var nodeMap = new Dictionary<int, MenuTreeNode>();
        foreach (var menu in all.OrderBy(m => m.Level).ThenBy(m => m.Sort))
        {
            nodeMap[menu.Id] = MapToTreeNode(menu, lookup);
        }

        // 第二遍：按 ParentId 挂载父子关系，ParentId 为空或父节点不在集合中的作为根节点
        var roots = new List<MenuTreeNode>();
        foreach (var menu in all)
        {
            var node = nodeMap[menu.Id];
            if (menu.ParentId == null || !nodeMap.ContainsKey(menu.ParentId.Value))
            {
                roots.Add(node);
            }
            else
            {
                nodeMap[menu.ParentId.Value].Children.Add(node);
            }
        }

        return roots.OrderBy(m => m.Sort).ToList();
    }

    private MenuTreeNode MapToTreeNode(Menus menu, Dictionary<int, Menus> lookup)
    {
        var node = new MenuTreeNode
        {
            Id = menu.Id,
            Name = menu.Name,
            MenuType = menu.MenuType,
            Path = menu.Path,
            Icon = menu.Icon,
            ParentId = menu.ParentId,
            FullPath = menu.FullPath,
            Level = menu.Level,
            Sort = menu.Sort,
            IsVisible = menu.IsVisible != false,
            IsEnabled = menu.IsEnabled != false,
            PermissionCode = menu.PermissionCode,
            ControllerAction = menu.ControllerAction,
            Remark = menu.Remark,
            HasChildren = lookup.Values.Any(m => m.ParentId == menu.Id),
            Children = new List<MenuTreeNode>()
        };
        return node;
    }

    // ==================== 统一权限树校验 ====================

    /// <summary>
    /// 统一权限树校验（Create/Modify 共用）：
    ///   1. 按钮节点（MenuType=3）：PermissionCode 必填、需匹配两段式 {resource}:{action}、全局唯一（排除自身）；
    ///      ControllerAction 选填，若填写必须存在于 ControllerActionScanner 扫描结果中。
    ///   2. 非按钮节点：不允许配置 PermissionCode/ControllerAction。
    ///   3. 菜单节点（MenuType=2）：Path 必填且全局唯一。
    /// </summary>
    private ResultModel<int> ValidatePermissionConfig(IMenusDAL dal, Menus model, int? excludeId = null)
    {
        if (model.MenuType == 3)
        {
            var code = model.PermissionCode?.Trim();
            if (string.IsNullOrWhiteSpace(code))
                return ResultModel<int>.BuildFail(1, "按钮节点必须配置权限编码（PermissionCode）");

            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[a-zA-Z][a-zA-Z0-9_]*(:[a-zA-Z][a-zA-Z0-9_]*)+$"))
                return ResultModel<int>.BuildFail(1, $"权限编码格式不正确：{code}（需为两段式及以上，如 users:edit）");

            var duplicated = dal.DbHelper.Connection.Query<int>(
                "SELECT COUNT(1) FROM Menus (nolock) WHERE PermissionCode = @Code AND Id != @ExcludeId",
                new { Code = code, ExcludeId = excludeId ?? 0 }).First();
            if (duplicated > 0)
                return ResultModel<int>.BuildFail(1, $"权限编码已存在：{code}");

            if (!string.IsNullOrWhiteSpace(model.ControllerAction)
                && !ControllerActionScanner.Exists(model.ControllerAction))
            {
                return ResultModel<int>.BuildFail(1, $"绑定的后端接口不存在：{model.ControllerAction}");
            }
        }
        else
        {
            // 目录/菜单节点不允许携带按钮属性
            model.PermissionCode = null;
            model.ControllerAction = null;

            if (model.MenuType == 2)
            {
                var path = model.Path?.Trim();
                if (string.IsNullOrWhiteSpace(path))
                    return ResultModel<int>.BuildFail(1, "菜单节点必须配置路由路径（Path）");

                var duplicatedPath = dal.DbHelper.Connection.Query<int>(
                    "SELECT COUNT(1) FROM Menus (nolock) WHERE Path = @Path AND MenuType = 2 AND Id != @ExcludeId",
                    new { Path = path, ExcludeId = excludeId ?? 0 }).First();
                if (duplicatedPath > 0)
                    return ResultModel<int>.BuildFail(1, $"路由路径已存在：{path}");
            }
        }
        return ResultModel<int>.BuildSuccess(0);
    }

    /// <summary>
    /// 清除角色权限编码缓存（PermissionCode/ControllerAction/IsEnabled/Path 变更会影响权限判定与按钮显隐）。
    /// 通过 ICacheService 清除全部角色的 role_perm_codes_ / role_menus_；用户级缓存短 TTL 自愈。
    /// </summary>
    private void ClearPermissionCaches()
    {
        // 按已知键规则清除（角色数通常为个位数到两位数，直接遍历删除）
        using (var session = Factory.OpenSession())
        {
            var conn = session.CreateDAL<IMenusDAL>().DbHelper.Connection;
            var roleIds = conn.Query<int>("SELECT Id FROM Roles (nolock)").ToList();
            foreach (var roleId in roleIds)
            {
                _globalCache.Remove($"role_perm_codes_{roleId}");
                _globalCache.Remove($"role_menus_{roleId}");
            }
        }
    }
}
