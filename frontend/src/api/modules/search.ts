import { http } from '../http'

export const searchApi = {
  status: () => http.get('/search-management/status'),
  rebuild: () => http.post('/search-management/rebuild'),
  tasks: (page = 1, pageSize = 20) => http.get('/search-management/tasks', { params: { page, pageSize } }),
  retry: (id: string) => http.post(`/search-management/tasks/${id}/retry`),
  cleanup: (retentionDays = 30) => http.delete('/search-management/tasks', { params: { retentionDays } })
}
