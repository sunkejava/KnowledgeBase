import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'

/**
 * 需要登录鉴权的业务请求客户端。
 */
export const http = axios.create({
  baseURL,
  timeout: 15000
})

/**
 * 无需登录的公共请求客户端，例如公开分享页面。
 */
export const publicHttp = axios.create({
  baseURL,
  timeout: 15000
})

http.interceptors.request.use(config => {
  const token = localStorage.getItem('kb_access_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

http.interceptors.response.use(
  response => response,
  error => {
    if (error?.response?.status === 401 && location.pathname !== '/login') {
      localStorage.removeItem('kb_access_token')
      localStorage.removeItem('kb_current_user')
      location.href = '/login'
    }
    return Promise.reject(error)
  }
)
