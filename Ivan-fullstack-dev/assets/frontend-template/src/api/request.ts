import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import { useUserStore } from '@/stores/user'

/** 后端统一响应格式，必须与 backend-template 中的 ApiResult<T> 保持一致 */
export interface ApiResult<T = unknown> {
  code: number
  message: string
  data: T
}

const request = axios.create({
  baseURL: '/api',
  timeout: 15000,
})

// 请求拦截：注入 token
request.interceptors.request.use((config) => {
  const userStore = useUserStore()
  if (userStore.token) {
    config.headers.Authorization = `Bearer ${userStore.token}`
  }
  return config
})

// 响应拦截：统一解包 ApiResult
request.interceptors.response.use(
  (response): any => {
    const res = response.data as ApiResult
    if (res.code === 0) {
      return res.data
    }
    // 401 / 403 统一视为认证/授权问题
    if (res.code === 401 || res.code === 403) {
      const userStore = useUserStore()
      userStore.clear()
      ElMessage.error(res.message || '权限不足，请重新登录')
      router.push('/login')
    } else {
      // 业务错误：使用后端返回的 message（精确错误信息从服务端来）
      ElMessage.error(res.message || '请求失败')
    }
    return Promise.reject(new Error(res.message || 'Error'))
  },
  (error) => {
    // 网络 / HTTP 非 200 状态码。
    // HTTP 401/403：token 失效（如浏览器 localStorage 残留旧项目签发的 token）或权限不足，
    // 清除本地登录态并跳转登录页，避免所有请求反复 401
    const status = error.response?.status
    if (status === 401 || status === 403) {
      const userStore = useUserStore()
      userStore.clear()
      ElMessage.error(status === 401 ? '登录已失效，请重新登录' : '权限不足，禁止访问')
      router.push('/login')
      return Promise.reject(error)
    }
    // 尝试从 error.response.data 解包服务端错误信息，
    // 若后端返回了 ApiResult 格式的错误（如 400 参数验证失败），优先使用其 message
    const apiResult = error.response?.data as ApiResult | undefined
    const errorMsg = apiResult?.message || error.message || '网络异常'
    ElMessage.error(errorMsg)
    return Promise.reject(error)
  }
)

export default request
