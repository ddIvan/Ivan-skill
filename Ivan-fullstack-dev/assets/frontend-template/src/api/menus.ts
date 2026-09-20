import request from './request'

export interface Menu {
  id: number
  name: string
  /** 节点类型：1=目录 2=菜单(页面) 3=按钮（统一权限树） */
  menuType: number
  path?: string
  icon?: string
  parentId?: number | null
  fullPath?: string
  level: number
  sort: number
  isVisible?: boolean
  isEnabled?: boolean
  /** 权限编码（按钮节点，两段式如 users:edit） */
  permissionCode?: string | null
  /** 绑定的后端接口（按钮节点，如 UserController.Update） */
  controllerAction?: string | null
  remark?: string | null
  createTime?: string
}

export interface MenuTreeNode {
  id: number
  name: string
  menuType: number
  path?: string
  icon?: string
  parentId?: number | null
  fullPath?: string
  level: number
  sort: number
  isVisible: boolean
  isEnabled: boolean
  permissionCode?: string | null
  controllerAction?: string | null
  remark?: string | null
  hasChildren: boolean
  children: MenuTreeNode[]
}

export interface MenuCreateRequest {
  name: string
  /** 节点类型：1=目录 2=菜单 3=按钮（默认 2） */
  menuType: number
  path?: string
  icon?: string
  parentId?: number | null
  sort?: number
  isVisible?: boolean
  isEnabled?: boolean
  permissionCode?: string
  controllerAction?: string
  remark?: string
}

export interface MenuUpdateRequest {
  name: string
  menuType: number
  path?: string
  icon?: string
  parentId?: number | null
  sort: number
  isVisible?: boolean
  isEnabled?: boolean
  permissionCode?: string
  controllerAction?: string
  remark?: string
}

export interface MenuSortRequest {
  id: number
  targetParentId?: number | null
  beforeId?: number | null
  afterId?: number | null
}

/** 获取全部菜单（平铺列表） */
export function getMenusAll() {
  return request.get<unknown, Menu[]>('/menus')
}

/** 获取菜单树（全量，含 children，统一树：目录/菜单/按钮） */
export function getMenuTree() {
  return request.get<unknown, MenuTreeNode[]>('/menus/tree')
}

/** 获取前端侧边栏菜单树（仅启用+可见节点） */
export function getFrontendTree() {
  return request.get<unknown, MenuTreeNode[]>('/menus/frontend-tree')
}

/** 懒加载：获取指定节点的子节点 */
export function getMenuChildren(parentId: number) {
  return request.get<unknown, MenuTreeNode[]>(`/menus/${parentId}/children`)
}

/** 获取单个菜单 */
export function getMenuById(id: number) {
  return request.get<unknown, Menu>(`/menus/${id}`)
}

/** 按路径查询 */
export function getMenuByPath(path: string) {
  return request.get<unknown, Menu>('/menus/by-path', { params: { path } })
}

/** 获取指定节点的所有子孙节点 */
export function getMenuDescendants(id: number) {
  return request.get<unknown, Menu[]>(`/menus/${id}/descendants`)
}

/** 新增菜单 */
export function createMenu(data: MenuCreateRequest) {
  return request.post<unknown, number>('/menus', data)
}

/** 更新菜单 */
export function updateMenu(id: number, data: MenuUpdateRequest) {
  return request.put<unknown, void>(`/menus/${id}`, data)
}

/** 启用/禁用菜单 */
export function setMenuEnabled(id: number, enabled: boolean) {
  return request.put<unknown, void>(`/menus/${id}/enabled`, null, { params: { enabled } })
}

/** 拖拽排序 */
export function reorderMenu(data: MenuSortRequest) {
  return request.put<unknown, void>('/menus/reorder', data)
}

/** 删除菜单（含子孙节点） */
export function deleteMenu(id: number) {
  return request.delete<unknown, void>(`/menus/${id}`)
}
