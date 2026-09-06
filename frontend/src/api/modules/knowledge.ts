import { http } from '../http'

export const knowledgeApi = {
  knowledgeBases: (page = 1, pageSize = 20, keyword = '') =>
    http.get('/knowledge-bases', { params: { page, pageSize, keyword: keyword || undefined } }),
  knowledgeBase: (id: string) => http.get(`/knowledge-bases/${id}`),
  createKnowledgeBase: (payload: unknown) => http.post('/knowledge-bases', payload),
  updateKnowledgeBase: (id: string, payload: unknown) => http.put(`/knowledge-bases/${id}`, payload),
  deleteKnowledgeBase: (id: string) => http.delete(`/knowledge-bases/${id}`),
  search: (keyword: string, take = 500) => http.get('/knowledge-assets/search', { params: { keyword, take } }),
  recent: (take = 500) => http.get('/knowledge-assets/recent', { params: { take } }),
  favorites: () => http.get('/knowledge-assets/favorites'),
  tags: () => http.get('/knowledge-assets/tags'),
  document: (id: string) => http.get(`/documents/${id}`)
}
