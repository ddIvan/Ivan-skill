import request from './request'

export interface User {
  id: number
  userName: string
  displayName?: string
  isEnabled?: boolean
  createTime?: string
  /** 角色 ID 列表（详情接口返回，编辑回显用） */
  roleIds?: number[]
}

export interface PageResult<T> {
  total: number
  items: T[]
}

export interface UserCreateData {
  userName: string
  password: string
  displayName?: string
  roleIds?: number[]
}

export interface UserUpdateData {
  displayName?: string
  isEnabled?: boolean
  /** null 表示不修改角色 */
  roleIds?: number[] | null
  newPassword?: string
}

/** 分页查询用户 */
export function getUsersPage(params: { keyword?: string; pageIndex?: number; pageSize?: number }) {
  return request.get<unknown, PageResult<User>>('/users', { params })
}

/** 根据 ID 查询用户（含角色 ID 列表） */
export function getUserById(id: number) {
  return request.get<unknown, User>(`/users/${id}`)
}

/** 新增用户 */
export function createUser(data: UserCreateData) {
  return request.post<unknown, number>('/users', data)
}

/** 更新用户（显示名/启用状态/角色/重置密码） */
export function updateUser(id: number, data: UserUpdateData) {
  return request.put<unknown, void>(`/users/${id}`, data)
}

/** 删除用户 */
export function deleteUser(id: number) {
  return request.delete<unknown, void>(`/users/${id}`)
}
