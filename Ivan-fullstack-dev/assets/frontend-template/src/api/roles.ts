import request from './request'

export interface Role {
  id: number
  name: string
  code: string
  description?: string
  createTime?: string
}

export interface RoleMenuButton {
  id: number
  roleId: number
  menuId: number
  buttonKey: string
}

export interface PageResult<T> {
  total: number
  items: T[]
}

/** 分页查询角色 */
export function getRolesPage(params: { keyword?: string; pageIndex?: number; pageSize?: number }) {
  return request.get<unknown, PageResult<Role>>('/roles', { params })
}

/** 获取所有角色 */
export function getRolesAll() {
  return request.get<unknown, Role[]>('/roles/all')
}

/** 获取单个角色 */
export function getRoleById(id: number) {
  return request.get<unknown, Role>(`/roles/${id}`)
}

/** 新增角色 */
export function createRole(data: Partial<Role>) {
  return request.post<unknown, number>('/roles', data)
}

/** 更新角色 */
export function updateRole(id: number, data: Partial<Role>) {
  return request.put<unknown, void>(`/roles/${id}`, data)
}

/** 删除角色 */
export function deleteRole(id: number) {
  return request.delete<unknown, void>(`/roles/${id}`)
}

/** 获取角色的菜单ID列表 */
export function getRoleMenus(id: number) {
  return request.get<unknown, number[]>(`/roles/${id}/menus`)
}

/** 保存角色的菜单权限 */
export function saveRoleMenus(id: number, menuIds: number[]) {
  return request.put<unknown, void>(`/roles/${id}/menus`, menuIds)
}

/** 获取角色的按钮权限 */
export function getRoleButtons(id: number) {
  return request.get<unknown, RoleMenuButton[]>(`/roles/${id}/buttons`)
}

/** 保存角色的按钮权限 */
export function saveRoleButtons(id: number, menuId: number, buttonKeys: string[]) {
  return request.put<unknown, void>(`/roles/${id}/buttons`, { menuId, buttonKeys })
}
