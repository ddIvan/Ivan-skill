import { defineStore } from 'pinia'
import { login as loginApi, type LoginRequest, type LoginResult } from '@/api/auth'

export const useUserStore = defineStore('user', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    userName: localStorage.getItem('userName') || '',
    displayName: localStorage.getItem('displayName') || '',
  }),
  actions: {
    async login(payload: LoginRequest) {
      const result = (await loginApi(payload)) as LoginResult
      this.token = result.token
      this.userName = result.userName
      this.displayName = result.displayName
      localStorage.setItem('token', result.token)
      localStorage.setItem('userName', result.userName)
      localStorage.setItem('displayName', result.displayName)
    },
    clear() {
      this.token = ''
      this.userName = ''
      this.displayName = ''
      localStorage.removeItem('token')
      localStorage.removeItem('userName')
      localStorage.removeItem('displayName')
    },
  },
})
