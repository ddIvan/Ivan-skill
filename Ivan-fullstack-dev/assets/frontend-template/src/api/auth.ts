import request from './request'

export interface LoginRequest {
  userName: string
  password: string
}

export interface LoginResult {
  token: string
  userName: string
  displayName: string
}

/** 登录 */
export function login(data: LoginRequest) {
  return request.post<unknown, LoginResult>('/auth/login', data)
}

/** 获取当前用户信息 */
export function getProfile() {
  return request.get<unknown, LoginResult>('/auth/profile')
}
