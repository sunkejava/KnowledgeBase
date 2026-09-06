import { http } from '../http'

export const authApi = {
  login: (userName: string, password: string) => http.post('/auth/login', { userName, password }),
  permissionProfile: () => http.get('/system/profile')
}
