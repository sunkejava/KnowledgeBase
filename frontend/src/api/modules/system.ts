import { http } from '../http'

export const systemApi = {
  users: () => http.get('/system/users'),
  roles: () => http.get('/system/roles'),
  departments: () => http.get('/system/departments'),
  organizations: () => http.get('/system/organizations'),
  menus: () => http.get('/system/menus'),
  auditLogs: (take = 500) => http.get('/system/audit-logs', { params: { take } }),
  createUser: (payload: unknown) => http.post('/system/users', payload),
  updateUser: (id: string, payload: unknown) => http.put(`/system/users/${id}`, payload),
  deleteUser: (id: string) => http.delete(`/system/users/${id}`),
  createRole: (payload: unknown) => http.post('/system/roles', payload),
  updateRole: (id: string, payload: unknown) => http.put(`/system/roles/${id}`, payload),
  deleteRole: (id: string) => http.delete(`/system/roles/${id}`)
}
