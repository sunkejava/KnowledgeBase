import axios from 'axios'

export const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api',
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
