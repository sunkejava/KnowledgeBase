import { http } from '../http'

export const knowledgeApi = {
  knowledgeBases: (page = 1, pageSize = 20, keyword = '') =>
    http.get('/knowledge-bases', { params: { page, pageSize, keyword: keyword || undefined } }),
  knowledgeBase: (id: string) => http.get(`/knowledge-bases/${id}`),
  createKnowledgeBase: (payload: unknown) => http.post('/knowledge-bases', payload),
  updateKnowledgeBase: (id: string, payload: unknown) => http.put(`/knowledge-bases/${id}`, payload),
  deleteKnowledgeBase: (id: string) => http.delete(`/knowledge-bases/${id}`),

  search: (keyword: string, page = 1, pageSize = 20, knowledgeBaseId?: string) =>
    http.get('/knowledge-assets/search', { params: { keyword, page, pageSize, knowledgeBaseId } }),
  recent: (page = 1, pageSize = 20, keyword = '') =>
    http.get('/knowledge-assets/recent', { params: { page, pageSize, keyword: keyword || undefined } }),
  favorites: (page = 1, pageSize = 20, keyword = '') =>
    http.get('/knowledge-assets/favorites', { params: { page, pageSize, keyword: keyword || undefined } }),
  tags: () => http.get('/knowledge-assets/tags'),
  document: (id: string) => http.get(`/documents/${id}`)
}
