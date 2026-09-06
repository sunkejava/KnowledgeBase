import { http } from '../http'
import type { PageRequest } from '../../types/table'

export const systemApi = {
  users: (query: PageRequest) => http.get('/system/users', { params: query }),
  roles: () => http.get('/system/roles'),
  departments: () => http.get('/system/departments'),
  organizations: () => http.get('/system/organizations'),
  menus: () => http.get('/system/menus'),
  auditLogs: (query: PageRequest) => http.get('/system/audit-logs', { params: query }),

  createUser: (payload: unknown) => http.post('/system/users', payload),
  updateUser: (id: string, payload: unknown) => http.put(`/system/users/${id}`, payload),
  deleteUser: (id: string) => http.delete(`/system/users/${id}`),

  createRole: (payload: unknown) => http.post('/system/roles', payload),
  updateRole: (id: string, payload: unknown) => http.put(`/system/roles/${id}`, payload),
  deleteRole: (id: string) => http.delete(`/system/roles/${id}`),

  createDepartment: (payload: unknown) => http.post('/system/departments', payload),
  updateDepartment: (id: string, payload: unknown) => http.put(`/system/departments/${id}`, payload),
  deleteDepartment: (id: string) => http.delete(`/system/departments/${id}`),

  createOrganization: (payload: unknown) => http.post('/system/organizations', payload),
  updateOrganization: (id: string, payload: unknown) => http.put(`/system/organizations/${id}`, payload),
  deleteOrganization: (id: string) => http.delete(`/system/organizations/${id}`),

  createMenu: (payload: unknown) => http.post('/system/menus', payload),
  updateMenu: (id: string, payload: unknown) => http.put(`/system/menus/${id}`, payload),
  deleteMenu: (id: string) => http.delete(`/system/menus/${id}`)
}
