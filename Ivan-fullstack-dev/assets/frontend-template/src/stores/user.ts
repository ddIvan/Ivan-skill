import { defineStore } from 'pinia'
import { login as loginApi, type LoginRequest, type LoginResult } from '@/api/auth'

export const useUserStore = defineStore('user', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    userName: localStorage.getItem('userName') || '',
    displayName: localStorage.getItem('displayName') || '',
    role: localStorage.getItem('role') || '',
    menuIds: JSON.parse(localStorage.getItem('menuIds') || '[]') as number[],
    buttonPermissions: JSON.parse(localStorage.getItem('buttonPermissions') || '{}') as Record<string, string[]>,
  }),
  getters: {
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
      this.role = result.role || ''
      this.menuIds = result.menuIds || []
      this.buttonPermissions = result.buttonPermissions || {}
      localStorage.setItem('token', result.token)
      localStorage.setItem('userName', result.userName)
      localStorage.setItem('displayName', result.displayName || '')
      localStorage.setItem('role', result.role || '')
      localStorage.setItem('menuIds', JSON.stringify(result.menuIds || []))
      localStorage.setItem('buttonPermissions', JSON.stringify(result.buttonPermissions || {}))
    },
    clear() {
      this.token = ''
      this.userName = ''
      this.displayName = ''
      this.role = ''
      this.menuIds = []
      this.buttonPermissions = {}
      localStorage.removeItem('token')
      localStorage.removeItem('userName')
      localStorage.removeItem('displayName')
      localStorage.removeItem('role')
      localStorage.removeItem('menuIds')
      localStorage.removeItem('buttonPermissions')
    },
  },
})
