import request from './request'

export interface Menu {
  id: number
  name: string
  path?: string
  icon?: string
  parentId?: number
  sort: number
  isVisible?: boolean
  createTime?: string
}

/** 获取菜单树 */
export function getMenuTree() {
  return request.get<unknown, Menu[]>('/menus/tree')
}

/** 获取所有菜单（平铺） */
export function getMenusAll() {
  return request.get<unknown, Menu[]>('/menus')
}

/** 获取单个菜单 */
export function getMenuById(id: number) {
  return request.get<unknown, Menu>(`/menus/${id}`)
}

/** 新增菜单 */
export function createMenu(data: Partial<Menu>) {
  return request.post<unknown, number>('/menus', data)
}

/** 更新菜单 */
export function updateMenu(id: number, data: Partial<Menu>) {
  return request.put<unknown, void>(`/menus/${id}`, data)
}

/** 删除菜单 */
export function deleteMenu(id: number) {
  return request.delete<unknown, void>(`/menus/${id}`)
}
