import { defineStore } from 'pinia'
import { login as loginApi, type LoginRequest, type LoginResult } from '@/api/auth'

export const useUserStore = defineStore('user', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    userName: localStorage.getItem('userName') || '',
    displayName: localStorage.getItem('displayName') || '',
    /** 角色编码列表（支持多角色） */
    roles: JSON.parse(localStorage.getItem('roles') || '[]') as string[],
    menuIds: JSON.parse(localStorage.getItem('menuIds') || '[]') as number[],
    buttonPermissions: JSON.parse(localStorage.getItem('buttonPermissions') || '{}') as Record<string, string[]>,
  }),
  getters: {
    /** 是否为 admin 角色 */
    isAdmin: (state) => state.roles.includes('admin'),
    /** 检查是否有指定按钮权限（menuPath 如 /users，buttonKey 如 add） */
    hasButton: (state) => (menuPath: string, buttonKey: string): boolean => {
      return state.buttonPermissions[menuPath]?.includes(buttonKey) ?? false
    },
  },
  actions: {
    async login(payload: LoginRequest) {
      const result = (await loginApi(payload)) as LoginResult
      this.token = result.token
      this.userName = result.userName
      this.displayName = result.displayName
      this.roles = result.roles || []
      this.menuIds = result.menuIds || []
      this.buttonPermissions = result.buttonPermissions || {}
      localStorage.setItem('token', result.token)
      localStorage.setItem('userName', result.userName)
      localStorage.setItem('displayName', result.displayName || '')
      localStorage.setItem('roles', JSON.stringify(this.roles))
      localStorage.setItem('menuIds', JSON.stringify(result.menuIds || []))
      localStorage.setItem('buttonPermissions', JSON.stringify(result.buttonPermissions || {}))
    },
    clear() {
      this.token = ''
      this.userName = ''
      this.displayName = ''
      this.roles = []
      this.menuIds = []
      this.buttonPermissions = {}
      localStorage.removeItem('token')
      localStorage.removeItem('userName')
      localStorage.removeItem('displayName')
      localStorage.removeItem('roles')
      localStorage.removeItem('menuIds')
      localStorage.removeItem('buttonPermissions')
    },
  },
})
